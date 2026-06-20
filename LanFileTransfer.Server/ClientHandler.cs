// 处理单个客户端连接和具体业务请求。
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using LanFileTransfer.Common;
using LanFileTransfer.Data;
using Microsoft.EntityFrameworkCore;

namespace LanFileTransfer.Server;

internal static class ClientHandler
{
    private const int FileBufferSize = 64 * 1024;
    private static readonly string StorageRoot = Path.Combine(AppContext.BaseDirectory, "ServerStorage");

    // 处理单个客户端连接，根据命令类型分发到对应业务逻辑。
    public static async Task HandleClientAsync(TcpClient client)
    {
        string clientAddress = client.Client.RemoteEndPoint?.ToString() ?? "未知客户端";
        Console.WriteLine($"客户端已连接：{clientAddress}");

        try
        {
            using (NetworkStream stream = client.GetStream())
            {
                CommandMessage message = await TcpMessageProtocol.ReceiveCommandAsync(stream);

                if (message.Type == MessageType.TestRequest)
                {
                    string requestText = message.GetField(0);
                    Console.WriteLine($"收到 {clientAddress} 测试消息：{requestText}");
                    await TcpMessageProtocol.SendCommandAsync(stream, MessageType.TestResponse, $"服务端已收到：{requestText}");
                }
                else if (message.Type == MessageType.RegisterRequest)
                {
                    await HandleRegisterAsync(message, stream);
                }
                else if (message.Type == MessageType.LoginRequest)
                {
                    await HandleLoginAsync(message, stream);
                }
                else if (message.Type == MessageType.UploadRequest)
                {
                    await HandleUploadAsync(message, stream, clientAddress);
                }
                else if (message.Type == MessageType.FileListRequest)
                {
                    await HandleFileListAsync(message, stream);
                }
                else if (message.Type == MessageType.DownloadRequest)
                {
                    await HandleDownloadAsync(message, stream, clientAddress);
                }
                else if (message.Type == MessageType.TransferRecordRequest)
                {
                    await HandleTransferRecordsAsync(message, stream);
                }
                else
                {
                    Console.WriteLine($"收到暂不支持的命令类型：{message.Type}");
                    await TcpMessageProtocol.SendCommandAsync(stream, MessageType.ErrorResponse, "服务端暂不支持该命令类型。", message.Type.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理客户端 {clientAddress} 时发生错误：{ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine($"客户端已断开：{clientAddress}");
        }
    }

    // 查询服务端已保存的文件列表，返回给客户端表格展示。
    private static async Task HandleFileListAsync(CommandMessage message, NetworkStream stream)
    {
        int.TryParse(message.GetField(0), out int userId);
        if (userId <= 0)
        {
            await TcpMessageProtocol.SendCommandAsync(stream, MessageType.FileListResponse, "false", "请先登录后再刷新文件列表。", "0");
            return;
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        bool userExists = await dbContext.Users.AnyAsync(user => user.Id == userId);
        if (!userExists)
        {
            await TcpMessageProtocol.SendCommandAsync(stream, MessageType.FileListResponse, "false", "当前用户不存在，请重新登录。", "0");
            return;
        }

        var files = await dbContext.FileRecords
            .Include(file => file.Uploader)
            .OrderByDescending(file => file.UploadedAt)
            .Select(file => new
            {
                file.Id,
                file.OriginalFileName,
                file.FileSize,
                file.ResourceType,
                UploaderName = file.Uploader == null ? "未知用户" : file.Uploader.Username,
                file.UploadedAt
            })
            .ToListAsync();

        List<string> fields = new List<string> { "true", "文件列表刷新成功。", files.Count.ToString() };
        foreach (var file in files)
        {
            fields.Add(file.Id.ToString());
            fields.Add(file.OriginalFileName);
            fields.Add(file.FileSize.ToString());
            fields.Add(file.ResourceType.ToString());
            fields.Add(file.UploaderName);
            fields.Add(file.UploadedAt.ToString("O"));
        }

        await TcpMessageProtocol.SendCommandAsync(stream, MessageType.FileListResponse, fields.ToArray());
    }

    // 查询当前用户的上传和下载记录，返回给客户端表格展示。
    private static async Task HandleTransferRecordsAsync(CommandMessage message, NetworkStream stream)
    {
        int.TryParse(message.GetField(0), out int userId);
        if (userId <= 0)
        {
            await TcpMessageProtocol.SendCommandAsync(stream, MessageType.TransferRecordResponse, "false", "请先登录后再查看传输记录。", "0");
            return;
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        bool userExists = await dbContext.Users.AnyAsync(user => user.Id == userId);
        if (!userExists)
        {
            await TcpMessageProtocol.SendCommandAsync(stream, MessageType.TransferRecordResponse, "false", "当前用户不存在，请重新登录。", "0");
            return;
        }

        var records = await dbContext.TransferRecords
            .Where(record => record.UserId == userId)
            .OrderByDescending(record => record.StartedAt)
            .Select(record => new
            {
                record.Id,
                record.UserId,
                record.FileId,
                record.TransferType,
                record.Status,
                record.BytesTransferred,
                record.ClientIp,
                record.StartedAt,
                record.FinishedAt
            })
            .ToListAsync();

        List<string> fields = new List<string> { "true", "传输记录刷新成功。", records.Count.ToString() };
        foreach (var record in records)
        {
            fields.Add(record.Id.ToString());
            fields.Add(record.UserId.ToString());
            fields.Add(record.FileId.ToString());
            fields.Add(record.TransferType.ToString());
            fields.Add(record.Status.ToString());
            fields.Add(record.BytesTransferred.ToString());
            fields.Add(record.ClientIp);
            fields.Add(record.StartedAt.ToString("O"));
            fields.Add(record.FinishedAt.ToString("O"));
        }

        await TcpMessageProtocol.SendCommandAsync(stream, MessageType.TransferRecordResponse, fields.ToArray());
    }

    // 处理单文件下载，先发送文件信息，再分块发送文件内容。
    private static async Task HandleDownloadAsync(CommandMessage message, NetworkStream stream, string clientAddress)
    {
        if (!int.TryParse(message.GetField(0), out int userId) || !int.TryParse(message.GetField(1), out int fileId))
        {
            await SendDownloadResponseAsync(stream, false, "下载请求参数不正确。", 0, string.Empty, ResourceType.File, 0);
            return;
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        FileRecord? fileRecord = await dbContext.FileRecords.FirstOrDefaultAsync(file => file.Id == fileId);
        if (fileRecord == null || !File.Exists(fileRecord.FilePath))
        {
            await SendDownloadResponseAsync(stream, false, "文件不存在或已被删除。", fileId, string.Empty, ResourceType.File, 0);
            return;
        }

        await SendDownloadResponseAsync(stream, true, "开始下载。", fileRecord.Id, fileRecord.OriginalFileName, fileRecord.ResourceType, fileRecord.FileSize);

        long bytesSent = await SendFileContentAsync(stream, fileRecord.FilePath);
        dbContext.TransferRecords.Add(new TransferRecord
        {
            UserId = userId,
            FileId = fileRecord.Id,
            TransferType = TransferType.Download,
            Status = TransferStatus.Success,
            BytesTransferred = bytesSent,
            ClientIp = clientAddress,
            StartedAt = DateTime.Now,
            FinishedAt = DateTime.Now
        });
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"文件下载完成：{fileRecord.OriginalFileName}，发送 {bytesSent} 字节");
    }

    // 从服务端本地文件读取字节并写入网络流。
    private static async Task<long> SendFileContentAsync(NetworkStream stream, string filePath)
    {
        byte[] buffer = new byte[FileBufferSize];
        long totalSent = 0;

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            while (true)
            {
                int readCount = await fileStream.ReadAsync(buffer);
                if (readCount == 0)
                {
                    break;
                }

                await stream.WriteAsync(buffer.AsMemory(0, readCount));
                totalSent += readCount;
            }
        }

        await stream.FlushAsync();
        return totalSent;
    }

    // 处理用户注册请求，检查重名后保存新用户。
    private static async Task HandleRegisterAsync(CommandMessage message, NetworkStream stream)
    {
        string username = message.GetField(0).Trim();
        string password = message.GetField(1);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await SendRegisterResponseAsync(stream, false, "用户名和密码不能为空。", null);
            return;
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();

        if (await dbContext.Users.AnyAsync(user => user.Username == username))
        {
            await SendRegisterResponseAsync(stream, false, "用户名已存在。", null);
            return;
        }

        User user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"用户注册成功：{username}");

        await SendRegisterResponseAsync(stream, true, "注册成功。", user.Id);
    }

    // 处理用户登录请求，查询用户并验证密码哈希。
    private static async Task HandleLoginAsync(CommandMessage message, NetworkStream stream)
    {
        string username = message.GetField(0).Trim();
        string password = message.GetField(1);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await SendLoginResponseAsync(stream, false, "用户名和密码不能为空。", null, null);
            return;
        }

        string passwordHash = HashPassword(password);

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        User? user = await dbContext.Users.FirstOrDefaultAsync(item => item.Username == username);

        if (user == null || user.PasswordHash != passwordHash)
        {
            await SendLoginResponseAsync(stream, false, "用户名或密码错误。", null, null);
            return;
        }

        Console.WriteLine($"用户登录成功：{username}");
        await SendLoginResponseAsync(stream, true, "登录成功。", user.Id, user.Username);
    }

    // 处理单文件上传，保存文件并写入文件记录和传输记录。
    private static async Task HandleUploadAsync(CommandMessage message, NetworkStream stream, string clientAddress)
    {
        if (!int.TryParse(message.GetField(0), out int userId) ||
            !Enum.TryParse(message.GetField(1), out ResourceType resourceType) ||
            !long.TryParse(message.GetField(2), out long fileSize))
        {
            await SendUploadResponseAsync(stream, false, "上传请求参数不正确。", null, 0);
            return;
        }

        string originalFileName = message.GetField(3);
        string extension = message.GetField(4);
        if (userId <= 0 || fileSize < 0 || string.IsNullOrWhiteSpace(originalFileName))
        {
            await SendUploadResponseAsync(stream, false, "上传请求参数不正确。", null, 0);
            return;
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        User? uploader = await dbContext.Users.FindAsync(userId);
        if (uploader == null)
        {
            await SendUploadResponseAsync(stream, false, "上传用户不存在，请先登录。", null, 0);
            return;
        }

        Directory.CreateDirectory(Path.Combine(StorageRoot, "temp"));
        string saveDate = DateTime.Now.ToString("yyyy-MM-dd");
        string fileDirectory = Path.Combine(StorageRoot, "files", saveDate);
        Directory.CreateDirectory(fileDirectory);

        extension = string.IsNullOrWhiteSpace(extension) ? Path.GetExtension(originalFileName) : extension;
        string storedFileName = $"{Guid.NewGuid():N}{extension}";
        string tempFilePath = Path.Combine(StorageRoot, "temp", storedFileName);
        string finalFilePath = Path.Combine(fileDirectory, storedFileName);

        long bytesReceived = 0;
        try
        {
            bytesReceived = await SaveUploadFileAsync(stream, tempFilePath, fileSize);
            if (bytesReceived != fileSize)
            {
                DeleteFileIfExists(tempFilePath);
                await SendUploadResponseAsync(stream, false, "文件大小校验失败。", null, bytesReceived);
                return;
            }

            File.Move(tempFilePath, finalFilePath, overwrite: false);

            FileRecord fileRecord = new FileRecord
            {
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                FilePath = finalFilePath,
                FileSize = fileSize,
                ResourceType = resourceType,
                UploaderId = userId
            };

            dbContext.FileRecords.Add(fileRecord);
            await dbContext.SaveChangesAsync();

            TransferRecord transferRecord = new TransferRecord
            {
                UserId = userId,
                FileId = fileRecord.Id,
                TransferType = TransferType.Upload,
                Status = TransferStatus.Success,
                BytesTransferred = bytesReceived,
                ClientIp = clientAddress,
                StartedAt = DateTime.Now,
                FinishedAt = DateTime.Now
            };

            dbContext.TransferRecords.Add(transferRecord);
            await dbContext.SaveChangesAsync();

            Console.WriteLine($"文件上传成功：{originalFileName} -> {finalFilePath}");
            await SendUploadResponseAsync(stream, true, "上传成功。", fileRecord.Id, bytesReceived);
        }
        catch (Exception ex)
        {
            DeleteFileIfExists(tempFilePath);
            Console.WriteLine($"文件上传失败：{ex.Message}");
            await SendUploadResponseAsync(stream, false, $"上传失败：{ex.Message}", null, bytesReceived);
        }
    }

    // 从网络流按指定文件大小读取内容并写入临时文件。
    private static async Task<long> SaveUploadFileAsync(NetworkStream stream, string filePath, long fileSize)
    {
        byte[] buffer = new byte[FileBufferSize];
        long totalRead = 0;

        using (FileStream fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            while (totalRead < fileSize)
            {
                int needRead = (int)Math.Min(buffer.Length, fileSize - totalRead);
                int readCount = await stream.ReadAsync(buffer.AsMemory(0, needRead));
                if (readCount == 0)
                {
                    break;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, readCount));
                totalRead += readCount;
            }
        }

        return totalRead;
    }

    // 发送登录响应命令。
    private static async Task SendLoginResponseAsync(NetworkStream stream, bool success, string message, int? userId, string? username)
    {
        await TcpMessageProtocol.SendCommandAsync(
            stream,
            MessageType.LoginResponse,
            ToProtocolBool(success),
            message,
            userId?.ToString() ?? string.Empty,
            username ?? string.Empty);
    }

    // 发送注册响应命令。
    private static async Task SendRegisterResponseAsync(NetworkStream stream, bool success, string message, int? userId)
    {
        await TcpMessageProtocol.SendCommandAsync(
            stream,
            MessageType.RegisterResponse,
            ToProtocolBool(success),
            message,
            userId?.ToString() ?? string.Empty);
    }

    // 发送上传响应命令。
    private static async Task SendUploadResponseAsync(NetworkStream stream, bool success, string message, int? fileId, long bytesTransferred)
    {
        await TcpMessageProtocol.SendCommandAsync(
            stream,
            MessageType.UploadResponse,
            ToProtocolBool(success),
            message,
            fileId?.ToString() ?? string.Empty,
            bytesTransferred.ToString());
    }

    // 发送下载响应命令。
    private static async Task SendDownloadResponseAsync(NetworkStream stream, bool success, string message, int fileId, string originalFileName, ResourceType resourceType, long fileSize)
    {
        await TcpMessageProtocol.SendCommandAsync(
            stream,
            MessageType.DownloadResponse,
            ToProtocolBool(success),
            message,
            fileId.ToString(),
            originalFileName,
            resourceType.ToString(),
            fileSize.ToString());
    }

    // 把布尔值转成协议中的小写文本。
    private static string ToProtocolBool(bool value)
    {
        return value ? "true" : "false";
    }

    // 如果临时文件存在则删除，避免上传失败留下残留文件。
    private static void DeleteFileIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    // 对密码做 SHA256 哈希，避免直接保存明文密码。
    private static string HashPassword(string password)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}

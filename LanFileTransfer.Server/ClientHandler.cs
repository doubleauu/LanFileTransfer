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
                    RegisterResponseDto response = await HandleRegisterAsync(message);
                    await SendRegisterResponseAsync(stream, response);
                }
                else if (message.Type == MessageType.LoginRequest)
                {
                    LoginResponseDto response = await HandleLoginAsync(message);
                    await SendLoginResponseAsync(stream, response);
                }
                else if (message.Type == MessageType.UploadRequest)
                {
                    UploadResponseDto response = await HandleUploadAsync(message, stream, clientAddress);
                    await SendUploadResponseAsync(stream, response);
                }
                else if (message.Type == MessageType.FileListRequest)
                {
                    FileListResponseDto response = await HandleFileListAsync(message);
                    await SendFileListResponseAsync(stream, response);
                }
                else if (message.Type == MessageType.DownloadRequest)
                {
                    await HandleDownloadAsync(message, stream, clientAddress);
                }
                else if (message.Type == MessageType.TransferRecordRequest)
                {
                    TransferRecordResponseDto response = await HandleTransferRecordsAsync(message);
                    await SendTransferRecordResponseAsync(stream, response);
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
    private static async Task<FileListResponseDto> HandleFileListAsync(CommandMessage message)
    {
        int.TryParse(message.GetField(0), out int userId);
        if (userId <= 0)
        {
            return new FileListResponseDto(false, "请先登录后再刷新文件列表。", new List<FileListItemDto>());
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        bool userExists = await dbContext.Users.AnyAsync(user => user.Id == userId);
        if (!userExists)
        {
            return new FileListResponseDto(false, "当前用户不存在，请重新登录。", new List<FileListItemDto>());
        }

        List<FileListItemDto> files = await dbContext.FileRecords
            .Include(file => file.Uploader)
            .OrderByDescending(file => file.UploadedAt)
            .Select(file => new FileListItemDto(
                file.Id,
                file.OriginalFileName,
                file.FileSize,
                file.ResourceType,
                file.Uploader == null ? "未知用户" : file.Uploader.Username,
                file.UploadedAt))
            .ToListAsync();

        return new FileListResponseDto(true, "文件列表刷新成功。", files);
    }

    // 查询当前用户的上传和下载记录，返回给客户端表格展示。
    private static async Task<TransferRecordResponseDto> HandleTransferRecordsAsync(CommandMessage message)
    {
        int.TryParse(message.GetField(0), out int userId);
        if (userId <= 0)
        {
            return new TransferRecordResponseDto(false, "请先登录后再查看传输记录。", new List<TransferRecordItemDto>());
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        bool userExists = await dbContext.Users.AnyAsync(user => user.Id == userId);
        if (!userExists)
        {
            return new TransferRecordResponseDto(false, "当前用户不存在，请重新登录。", new List<TransferRecordItemDto>());
        }

        List<TransferRecordItemDto> records = await dbContext.TransferRecords
            .Where(record => record.UserId == userId)
            .OrderByDescending(record => record.StartedAt)
            .Select(record => new TransferRecordItemDto(
                record.Id,
                record.UserId,
                record.FileId,
                record.TransferType,
                record.Status,
                record.BytesTransferred,
                record.ClientIp,
                record.StartedAt,
                record.FinishedAt))
            .ToListAsync();

        return new TransferRecordResponseDto(true, "传输记录刷新成功。", records);
    }

    // 处理单文件下载，先发送文件信息，再分块发送文件内容。
    private static async Task HandleDownloadAsync(CommandMessage message, NetworkStream stream, string clientAddress)
    {
        DownloadRequestDto? request = ParseDownloadRequest(message);
        if (request == null)
        {
            await SendDownloadResponseAsync(stream, new DownloadResponseDto(false, "下载请求参数不正确。", 0, string.Empty, ResourceType.File, 0));
            return;
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        FileRecord? fileRecord = await dbContext.FileRecords.FirstOrDefaultAsync(file => file.Id == request.FileId);
        if (fileRecord == null || !File.Exists(fileRecord.FilePath))
        {
            await SendDownloadResponseAsync(stream, new DownloadResponseDto(false, "文件不存在或已被删除。", request.FileId, string.Empty, ResourceType.File, 0));
            return;
        }

        DownloadResponseDto successResponse = new DownloadResponseDto(
            true,
            "开始下载。",
            fileRecord.Id,
            fileRecord.OriginalFileName,
            fileRecord.ResourceType,
            fileRecord.FileSize);
        await SendDownloadResponseAsync(stream, successResponse);

        long bytesSent = await SendFileContentAsync(stream, fileRecord.FilePath);
        dbContext.TransferRecords.Add(new TransferRecord
        {
            UserId = request.UserId,
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
    private static async Task<RegisterResponseDto> HandleRegisterAsync(CommandMessage message)
    {
        RegisterRequestDto? request = ParseRegisterRequest(message);
        if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new RegisterResponseDto(false, "用户名和密码不能为空。", null);
        }

        string username = request.Username.Trim();
        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();

        if (await dbContext.Users.AnyAsync(user => user.Username == username))
        {
            return new RegisterResponseDto(false, "用户名已存在。", null);
        }

        User user = new User
        {
            Username = username,
            PasswordHash = HashPassword(request.Password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"用户注册成功：{username}");

        return new RegisterResponseDto(true, "注册成功。", user.Id);
    }

    // 处理用户登录请求，查询用户并验证密码哈希。
    private static async Task<LoginResponseDto> HandleLoginAsync(CommandMessage message)
    {
        LoginRequestDto? request = ParseLoginRequest(message);
        if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResponseDto(false, "用户名和密码不能为空。", null, null);
        }

        string username = request.Username.Trim();
        string passwordHash = HashPassword(request.Password);

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        User? user = await dbContext.Users.FirstOrDefaultAsync(item => item.Username == username);

        if (user == null || user.PasswordHash != passwordHash)
        {
            return new LoginResponseDto(false, "用户名或密码错误。", null, null);
        }

        Console.WriteLine($"用户登录成功：{username}");
        return new LoginResponseDto(true, "登录成功。", user.Id, user.Username);
    }

    // 处理单文件上传，保存文件并写入文件记录和传输记录。
    private static async Task<UploadResponseDto> HandleUploadAsync(CommandMessage message, NetworkStream stream, string clientAddress)
    {
        UploadRequestDto? request = ParseUploadRequest(message);
        if (request == null || request.UserId <= 0 || request.FileSize < 0 || string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            return new UploadResponseDto(false, "上传请求参数不正确。", null, 0);
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        User? uploader = await dbContext.Users.FindAsync(request.UserId);
        if (uploader == null)
        {
            return new UploadResponseDto(false, "上传用户不存在，请先登录。", null, 0);
        }

        Directory.CreateDirectory(Path.Combine(StorageRoot, "temp"));
        string saveDate = DateTime.Now.ToString("yyyy-MM-dd");
        string fileDirectory = Path.Combine(StorageRoot, "files", saveDate);
        Directory.CreateDirectory(fileDirectory);

        string extension = string.IsNullOrWhiteSpace(request.Extension) ? Path.GetExtension(request.OriginalFileName) : request.Extension;
        string storedFileName = $"{Guid.NewGuid():N}{extension}";
        string tempFilePath = Path.Combine(StorageRoot, "temp", storedFileName);
        string finalFilePath = Path.Combine(fileDirectory, storedFileName);

        long bytesReceived = 0;
        try
        {
            bytesReceived = await SaveUploadFileAsync(stream, tempFilePath, request.FileSize);
            if (bytesReceived != request.FileSize)
            {
                DeleteFileIfExists(tempFilePath);
                return new UploadResponseDto(false, "文件大小校验失败。", null, bytesReceived);
            }

            File.Move(tempFilePath, finalFilePath, overwrite: false);

            FileRecord fileRecord = new FileRecord
            {
                OriginalFileName = request.OriginalFileName,
                StoredFileName = storedFileName,
                FilePath = finalFilePath,
                FileSize = request.FileSize,
                FileHash = request.FileHash,
                ResourceType = request.ResourceType,
                UploaderId = request.UserId
            };

            dbContext.FileRecords.Add(fileRecord);
            await dbContext.SaveChangesAsync();

            TransferRecord transferRecord = new TransferRecord
            {
                UserId = request.UserId,
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

            Console.WriteLine($"文件上传成功：{request.OriginalFileName} -> {finalFilePath}");
            return new UploadResponseDto(true, "上传成功。", fileRecord.Id, bytesReceived);
        }
        catch (Exception ex)
        {
            DeleteFileIfExists(tempFilePath);
            Console.WriteLine($"文件上传失败：{ex.Message}");
            return new UploadResponseDto(false, $"上传失败：{ex.Message}", null, bytesReceived);
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

    // 解析登录命令字段。
    private static LoginRequestDto? ParseLoginRequest(CommandMessage message)
    {
        if (message.Fields.Count < 2)
        {
            return null;
        }

        return new LoginRequestDto(message.GetField(0), message.GetField(1));
    }

    // 解析注册命令字段。
    private static RegisterRequestDto? ParseRegisterRequest(CommandMessage message)
    {
        if (message.Fields.Count < 2)
        {
            return null;
        }

        return new RegisterRequestDto(message.GetField(0), message.GetField(1));
    }

    // 解析上传命令字段。
    private static UploadRequestDto? ParseUploadRequest(CommandMessage message)
    {
        if (message.Fields.Count < 6)
        {
            return null;
        }

        if (!int.TryParse(message.GetField(0), out int userId) ||
            !Enum.TryParse(message.GetField(2), out ResourceType resourceType) ||
            !long.TryParse(message.GetField(3), out long fileSize))
        {
            return null;
        }

        string fileHash = message.GetField(6);
        return new UploadRequestDto(
            userId,
            message.GetField(1),
            resourceType,
            fileSize,
            message.GetField(4),
            message.GetField(5),
            string.IsNullOrWhiteSpace(fileHash) ? null : fileHash);
    }

    // 解析下载命令字段。
    private static DownloadRequestDto? ParseDownloadRequest(CommandMessage message)
    {
        if (message.Fields.Count < 2)
        {
            return null;
        }

        if (!int.TryParse(message.GetField(0), out int userId) || !int.TryParse(message.GetField(1), out int fileId))
        {
            return null;
        }

        return new DownloadRequestDto(userId, fileId);
    }

    // 发送登录响应命令。
    private static async Task SendLoginResponseAsync(NetworkStream stream, LoginResponseDto response)
    {
        await TcpMessageProtocol.SendCommandAsync(
            stream,
            MessageType.LoginResponse,
            ToProtocolBool(response.Success),
            response.Message,
            response.UserId?.ToString() ?? string.Empty,
            response.Username ?? string.Empty);
    }

    // 发送注册响应命令。
    private static async Task SendRegisterResponseAsync(NetworkStream stream, RegisterResponseDto response)
    {
        await TcpMessageProtocol.SendCommandAsync(
            stream,
            MessageType.RegisterResponse,
            ToProtocolBool(response.Success),
            response.Message,
            response.UserId?.ToString() ?? string.Empty);
    }

    // 发送上传响应命令。
    private static async Task SendUploadResponseAsync(NetworkStream stream, UploadResponseDto response)
    {
        await TcpMessageProtocol.SendCommandAsync(
            stream,
            MessageType.UploadResponse,
            ToProtocolBool(response.Success),
            response.Message,
            response.FileId?.ToString() ?? string.Empty,
            response.BytesTransferred.ToString());
    }

    // 发送下载响应命令。
    private static async Task SendDownloadResponseAsync(NetworkStream stream, DownloadResponseDto response)
    {
        await TcpMessageProtocol.SendCommandAsync(
            stream,
            MessageType.DownloadResponse,
            ToProtocolBool(response.Success),
            response.Message,
            response.FileId.ToString(),
            response.OriginalFileName,
            response.ResourceType.ToString(),
            response.FileSize.ToString());
    }

    // 发送文件列表响应命令。
    private static async Task SendFileListResponseAsync(NetworkStream stream, FileListResponseDto response)
    {
        List<string> fields = new List<string>();
        fields.Add(ToProtocolBool(response.Success));
        fields.Add(response.Message);
        fields.Add(response.Files.Count.ToString());

        foreach (FileListItemDto file in response.Files)
        {
            fields.Add(file.FileId.ToString());
            fields.Add(file.OriginalFileName);
            fields.Add(file.FileSize.ToString());
            fields.Add(file.ResourceType.ToString());
            fields.Add(file.UploaderName);
            fields.Add(file.UploadedAt.ToString("O"));
        }

        await TcpMessageProtocol.SendCommandAsync(stream, MessageType.FileListResponse, fields.ToArray());
    }

    // 发送传输记录响应命令。
    private static async Task SendTransferRecordResponseAsync(NetworkStream stream, TransferRecordResponseDto response)
    {
        List<string> fields = new List<string>();
        fields.Add(ToProtocolBool(response.Success));
        fields.Add(response.Message);
        fields.Add(response.Records.Count.ToString());

        foreach (TransferRecordItemDto record in response.Records)
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

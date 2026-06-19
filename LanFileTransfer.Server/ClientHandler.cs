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

    // 处理单个客户端连接，根据消息类型分发到对应业务逻辑。
    public static async Task HandleClientAsync(TcpClient client)
    {
        string clientAddress = client.Client.RemoteEndPoint?.ToString() ?? "未知客户端";
        Console.WriteLine($"客户端已连接：{clientAddress}");

        try
        {
            await using NetworkStream stream = client.GetStream();
            ReceivedMessage message = await TcpMessageProtocol.ReceiveAsync(stream);

            if (message.Type == MessageType.TestRequest)
            {
                TestMessageDto? request = message.ReadBody<TestMessageDto>();
                Console.WriteLine($"收到 {clientAddress} 测试消息：{request?.Content}");

                TestMessageDto response = new($"服务端已收到：{request?.Content}");
                await TcpMessageProtocol.SendAsync(stream, MessageType.TestResponse, response);
            }
            else if (message.Type == MessageType.RegisterRequest)
            {
                RegisterResponseDto response = await HandleRegisterAsync(message);
                await TcpMessageProtocol.SendAsync(stream, MessageType.RegisterResponse, response);
            }
            else if (message.Type == MessageType.LoginRequest)
            {
                LoginResponseDto response = await HandleLoginAsync(message);
                await TcpMessageProtocol.SendAsync(stream, MessageType.LoginResponse, response);
            }
            else if (message.Type == MessageType.UploadRequest)
            {
                UploadResponseDto response = await HandleUploadAsync(message, stream, clientAddress);
                await TcpMessageProtocol.SendAsync(stream, MessageType.UploadResponse, response);
            }
            else if (message.Type == MessageType.FileListRequest)
            {
                FileListResponseDto response = await HandleFileListAsync(message);
                await TcpMessageProtocol.SendAsync(stream, MessageType.FileListResponse, response);
            }
            else
            {
                Console.WriteLine($"收到暂不支持的消息类型：{message.Type}");
                ErrorResponseDto response = new("服务端暂不支持该消息类型。", message.Type.ToString());
                await TcpMessageProtocol.SendAsync(stream, MessageType.ErrorResponse, response);
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
    private static async Task<FileListResponseDto> HandleFileListAsync(ReceivedMessage message)
    {
        FileListRequestDto? request = message.ReadBody<FileListRequestDto>();
        if (request == null || request.UserId <= 0)
        {
            return new FileListResponseDto(false, "请先登录后再刷新文件列表。", Array.Empty<FileListItemDto>());
        }

        await using LanFileTransferDbContext dbContext = ServerDatabase.CreateDbContext();
        bool userExists = await dbContext.Users.AnyAsync(user => user.Id == request.UserId);
        if (!userExists)
        {
            return new FileListResponseDto(false, "当前用户不存在，请重新登录。", Array.Empty<FileListItemDto>());
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

    // 处理用户注册请求，检查重名后保存新用户。
    private static async Task<RegisterResponseDto> HandleRegisterAsync(ReceivedMessage message)
    {
        RegisterRequestDto? request = message.ReadBody<RegisterRequestDto>();
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

        User user = new()
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
    private static async Task<LoginResponseDto> HandleLoginAsync(ReceivedMessage message)
    {
        LoginRequestDto? request = message.ReadBody<LoginRequestDto>();
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
    private static async Task<UploadResponseDto> HandleUploadAsync(ReceivedMessage message, NetworkStream stream, string clientAddress)
    {
        UploadRequestDto? request = message.ReadBody<UploadRequestDto>();
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

            FileRecord fileRecord = new()
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

            TransferRecord transferRecord = new()
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

        await using FileStream fileStream = new(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
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

        return totalRead;
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

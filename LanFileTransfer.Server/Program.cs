// 服务端程序入口，负责启动基础 TCP 监听并返回测试响应。
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using LanFileTransfer.Common;
using LanFileTransfer.Data;
using Microsoft.EntityFrameworkCore;

namespace LanFileTransfer.Server;

internal class Program
{
    private const int DefaultPort = 5000;  // 默认监听5000端口
    private static readonly string DatabasePath = Path.Combine(AppContext.BaseDirectory, "LanFileTransfer.db");

    private static async Task Main(string[] args)
    {
        InitializeDatabase();

        int port = GetPort(args);
        TcpListener listener = new(IPAddress.Any, port);

        listener.Start();
        Console.WriteLine($"服务端已启动，监听端口：{port}");
        Console.WriteLine("按 Ctrl + C 结束服务端。");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private static void InitializeDatabase()
    {
        // 作业项目早期先用 EnsureCreated 快速生成表结构，后续需要迁移时再改为 Migrations。
        using LanFileTransferDbContext dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
        VerifyDatabaseReadWrite(dbContext);

        Console.WriteLine($"数据库已就绪：{DatabasePath}");
    }

    private static LanFileTransferDbContext CreateDbContext()
    {
        DbContextOptions<LanFileTransferDbContext> options = new DbContextOptionsBuilder<LanFileTransferDbContext>()
            .UseSqlite($"Data Source={DatabasePath}")
            .Options;

        return new LanFileTransferDbContext(options);
    }

    private static void VerifyDatabaseReadWrite(LanFileTransferDbContext dbContext)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        string testUsername = $"__db_test_{Guid.NewGuid():N}";

        dbContext.Users.Add(new User
        {
            Username = testUsername,
            PasswordHash = "test"
        });
        dbContext.SaveChanges();

        // 查询刚写入的临时用户，验证 EF Core 新增和查询都能正常工作。
        bool canReadBack = dbContext.Users.Any(user => user.Username == testUsername);
        if (!canReadBack)
        {
            throw new InvalidOperationException("数据库读写验证失败。");
        }

        transaction.Rollback();
    }

    private static int GetPort(string[] args)
    {
        // 支持通过命令行参数临时指定端口，未指定时使用默认端口。
        if (args.Length > 0 && int.TryParse(args[0], out int port))
        {
            return port;
        }

        return DefaultPort;
    }

    private static async Task HandleClientAsync(TcpClient client)
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

    private static async Task<RegisterResponseDto> HandleRegisterAsync(ReceivedMessage message)
    {
        RegisterRequestDto? request = message.ReadBody<RegisterRequestDto>();
        if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new RegisterResponseDto(false, "用户名和密码不能为空。", null);
        }

        string username = request.Username.Trim();
        await using LanFileTransferDbContext dbContext = CreateDbContext();

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

    private static async Task<LoginResponseDto> HandleLoginAsync(ReceivedMessage message)
    {
        LoginRequestDto? request = message.ReadBody<LoginRequestDto>();
        if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResponseDto(false, "用户名和密码不能为空。", null, null);
        }

        string username = request.Username.Trim();
        string passwordHash = HashPassword(request.Password);

        await using LanFileTransferDbContext dbContext = CreateDbContext();
        User? user = await dbContext.Users.FirstOrDefaultAsync(item => item.Username == username);

        if (user == null || user.PasswordHash != passwordHash)
        {
            return new LoginResponseDto(false, "用户名或密码错误。", null, null);
        }

        Console.WriteLine($"用户登录成功：{username}");
        return new LoginResponseDto(true, "登录成功。", user.Id, user.Username);
    }

    private static string HashPassword(string password)
    {
        // 课程项目先用 SHA256 保存密码哈希，避免数据库里直接保存明文密码。
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}

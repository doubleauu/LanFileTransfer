// 服务端程序入口，负责启动基础 TCP 监听并返回测试响应。
using System.Net;
using System.Net.Sockets;
using LanFileTransfer.Common;
using LanFileTransfer.Data;
using Microsoft.EntityFrameworkCore;

namespace LanFileTransfer.Server;

internal class Program
{
    private const int DefaultPort = 5000;  // 默认监听5000端口

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
        string databasePath = Path.Combine(AppContext.BaseDirectory, "LanFileTransfer.db");
        DbContextOptions<LanFileTransferDbContext> options = new DbContextOptionsBuilder<LanFileTransferDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        // 作业项目早期先用 EnsureCreated 快速生成表结构，后续需要迁移时再改为 Migrations。
        using LanFileTransferDbContext dbContext = new(options);
        dbContext.Database.EnsureCreated();
        VerifyDatabaseReadWrite(dbContext);

        Console.WriteLine($"数据库已就绪：{databasePath}");
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

            // 阶段四先支持连接测试消息，后续业务请求继续走同一协议。
            if (message.Type == MessageType.TestRequest)
            {
                TestMessageDto? request = message.ReadBody<TestMessageDto>();
                Console.WriteLine($"收到 {clientAddress} 测试消息：{request?.Content}");

                TestMessageDto response = new($"服务端已收到：{request?.Content}");
                await TcpMessageProtocol.SendAsync(stream, MessageType.TestResponse, response);
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
}

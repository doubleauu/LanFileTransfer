// 服务端程序入口，负责初始化数据库并启动 TCP 监听。
using System.Net;
using System.Net.Sockets;

namespace LanFileTransfer.Server;

internal class Program
{
    private const int DefaultPort = 5000;  // 默认监听5000端口

    // 初始化数据库并启动 TCP 监听循环。
    private static async Task Main(string[] args)
    {
        ServerDatabase.InitializeDatabase();  // 初始化数据库

        int port = GetPort(args);   // 获取命令行中的指定端口
        TcpListener listener = new(IPAddress.Any, port);

        listener.Start();
        Console.WriteLine($"服务端已启动，监听端口：{port}");
        Console.WriteLine("按 Ctrl + C 结束服务端。");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();  // 异步等待每一个客户端
            // 多线程 Task 处理每一个客户端且不等待，使得支持多客户端同时连接（放到线程池中，默认后台线程）
            _ = Task.Run(() => ClientHandler.HandleClientAsync(client));
        }
    }

    // 从命令行读取端口，未提供时使用默认端口。
    private static int GetPort(string[] args)
    {
        // 支持通过命令行参数临时指定端口，未指定时使用默认端口。
        if (args.Length > 0 && int.TryParse(args[0], out int port))
        {
            return port;
        }

        return DefaultPort;
    }
}

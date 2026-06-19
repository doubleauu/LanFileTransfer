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
        ServerDatabase.InitializeDatabase();

        int port = GetPort(args);
        TcpListener listener = new(IPAddress.Any, port);

        listener.Start();
        Console.WriteLine($"服务端已启动，监听端口：{port}");
        Console.WriteLine("按 Ctrl + C 结束服务端。");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
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

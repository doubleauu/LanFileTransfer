// 服务端程序入口，负责启动基础 TCP 监听并返回测试响应。
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LanFileTransfer.Server;

internal class Program
{
    private const int DefaultPort = 5000;  // 默认监听5000端口

    private static async Task Main(string[] args)
    {
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
            using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
            await using StreamWriter writer = new(stream, Encoding.UTF8, leaveOpen: true)
            {
                AutoFlush = true
            };

            // 阶段三只验证简单文本消息，后续阶段再替换为统一通信协议。
            string? message = await reader.ReadLineAsync();
            Console.WriteLine($"收到 {clientAddress} 消息：{message}");

            await writer.WriteLineAsync($"服务端已收到：{message}");
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

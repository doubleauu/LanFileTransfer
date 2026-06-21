// 封装客户端与服务端的基础通信操作，避免窗体代码直接处理网络流细节。
using System.Net.Sockets;
using LanFileTransfer.Common;

namespace LanFileTransfer.Client;

internal class ClientConnection
{
    private const int FileBufferSize = 64 * 1024;  // 文件缓冲区：64KB

    // 建立短连接，客户端发送一条命令并接收服务端响应。为了方便之后的接受，所以给客户端封装了一个发送函数
    public async Task<CommandMessage> SendCommandAsync(string serverIp, int port, MessageType messageType, params string[] fields)
    {
        // using 自动释放，大括号限制生命周期
        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync(serverIp, port);

            using (NetworkStream stream = client.GetStream())
            {
                await TcpMessageProtocol.SendCommandAsync(stream, messageType, fields);
                return await TcpMessageProtocol.ReceiveCommandAsync(stream);  // 显式实现：客户端发送消息之后就开始等待接受，而服务端式通过死循环一直监听
            }
        }
    }

    // 上传指定文件，先发送上传命令，再分块发送文件内容。
    public async Task<CommandMessage> UploadFileAsync(
        string serverIp,
        int port,
        int userId,
        ResourceType resourceType,
        long fileSize,
        string originalFileName,
        string extension,
        string filePath,
        Action<int>? reportProgress)  // 一个回调函数，用于更新上传进度
    {
        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync(serverIp, port);

            using (NetworkStream stream = client.GetStream())
            {
                // 发送文件元信息：
                await TcpMessageProtocol.SendCommandAsync(
                    stream,
                    MessageType.UploadRequest,
                    userId.ToString(),
                    resourceType.ToString(),
                    fileSize.ToString(),
                    originalFileName,
                    extension);

                await SendFileContentAsync(stream, filePath, fileSize, reportProgress);
                return await TcpMessageProtocol.ReceiveCommandAsync(stream);
            }
        }
    }

    // 下载指定文件，先发送下载命令，再根据响应读取文件内容。
    public async Task<CommandMessage> DownloadFileAsync(string serverIp, int port, int userId, int fileId, string savePath, Action<int>? reportProgress)
    {
        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync(serverIp, port);

            using (NetworkStream stream = client.GetStream())
            {
                await TcpMessageProtocol.SendCommandAsync(
                    stream,
                    MessageType.DownloadRequest,
                    userId.ToString(),
                    fileId.ToString());

                CommandMessage response = await TcpMessageProtocol.ReceiveCommandAsync(stream);
                if (response.Type != MessageType.DownloadResponse || !IsProtocolTrue(response.GetField(0)))
                {
                    return response;
                }

                long.TryParse(response.GetField(5), out long fileSize);
                await ReceiveFileContentAsync(stream, savePath, fileSize, reportProgress);
                return response;
            }
        }
    }

    // 上传文件具体内容，从本地文件读取字节并写入网络流，同时回调上传进度。
    private static async Task SendFileContentAsync(NetworkStream stream, string filePath, long fileSize, Action<int>? reportProgress)
    {
        byte[] buffer = new byte[FileBufferSize];
        long totalSent = 0;  // 已发送字节数

        // 以只读形式打开一个文件流，且允许其他程序同时读取
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            while (true)
            {
                int readCount = await fileStream.ReadAsync(buffer);
                if (readCount == 0)
                {
                    break;
                }

                await stream.WriteAsync(buffer.AsMemory(0, readCount));  // 只发送有效数据，避免拷贝，提高性能
                totalSent += readCount;

                if (fileSize > 0)
                {
                    int progress = Math.Min(100, (int)(totalSent / fileSize * 100));
                    reportProgress?.Invoke(progress);  // 回到UI进程更新
                }
            }
        }

        await stream.FlushAsync();  // 强制将缓冲区数据发送出去
    }

    // 从网络流读取指定大小的文件内容并保存到本地。
    private static async Task ReceiveFileContentAsync(NetworkStream stream, string savePath, long fileSize, Action<int>? reportProgress)
    {
        byte[] buffer = new byte[FileBufferSize];
        long totalRead = 0;

        using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            while (totalRead < fileSize)
            {
                int needRead = (int)Math.Min(buffer.Length, fileSize - totalRead);
                int readCount = await stream.ReadAsync(buffer.AsMemory(0, needRead));
                if (readCount == 0)
                {
                    throw new EndOfStreamException("下载连接提前断开。");
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, readCount));
                totalRead += readCount;

                if (fileSize > 0)
                {
                    int progress = Math.Min(100, (int)(totalRead * 100 / fileSize));
                    reportProgress?.Invoke(progress);
                }
            }
        }
    }

    // 判断协议中的布尔文本是否表示 true。
    private static bool IsProtocolTrue(string value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}

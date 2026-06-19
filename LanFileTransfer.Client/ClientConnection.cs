// 封装客户端与服务端的基础通信操作，避免窗体代码直接处理网络流细节。
using System.Net.Sockets;
using LanFileTransfer.Common;

namespace LanFileTransfer.Client;

internal class ClientConnection
{
    private const int FileBufferSize = 64 * 1024;

    // 建立短连接，发送一个结构化请求并接收服务端响应。
    public async Task<ReceivedMessage> SendRequestAsync<T>(string serverIp, int port, MessageType messageType, T request)
    {
        using TcpClient client = new();
        await client.ConnectAsync(serverIp, port);

        await using NetworkStream stream = client.GetStream();
        await TcpMessageProtocol.SendAsync(stream, messageType, request);

        return await TcpMessageProtocol.ReceiveAsync(stream);
    }

    // 上传指定文件，先发送上传元数据，再分块发送文件内容。
    public async Task<ReceivedMessage> UploadFileAsync(string serverIp, int port, UploadRequestDto request, string filePath, Action<int>? reportProgress)
    {
        using TcpClient client = new();
        await client.ConnectAsync(serverIp, port);

        await using NetworkStream stream = client.GetStream();
        await TcpMessageProtocol.SendAsync(stream, MessageType.UploadRequest, request);
        await SendFileContentAsync(stream, filePath, request.FileSize, reportProgress);

        return await TcpMessageProtocol.ReceiveAsync(stream);
    }

    // 下载指定文件，先接收下载响应，再按服务端返回的文件大小读取内容。
    public async Task<DownloadResponseDto?> DownloadFileAsync(string serverIp, int port, DownloadRequestDto request, string savePath, Action<int>? reportProgress)
    {
        using TcpClient client = new();
        await client.ConnectAsync(serverIp, port);

        await using NetworkStream stream = client.GetStream();
        await TcpMessageProtocol.SendAsync(stream, MessageType.DownloadRequest, request);

        ReceivedMessage responseMessage = await TcpMessageProtocol.ReceiveAsync(stream);
        DownloadResponseDto? response = responseMessage.ReadBody<DownloadResponseDto>();
        if (response?.Success != true)
        {
            return response;
        }

        await ReceiveFileContentAsync(stream, savePath, response.FileSize, reportProgress);
        return response;
    }

    // 从本地文件读取字节并写入网络流，同时回调上传进度。
    private static async Task SendFileContentAsync(NetworkStream stream, string filePath, long fileSize, Action<int>? reportProgress)
    {
        byte[] buffer = new byte[FileBufferSize];
        long totalSent = 0;

        await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        while (true)
        {
            int readCount = await fileStream.ReadAsync(buffer);
            if (readCount == 0)
            {
                break;
            }

            await stream.WriteAsync(buffer.AsMemory(0, readCount));
            totalSent += readCount;

            if (fileSize > 0)
            {
                int progress = Math.Min(100, (int)(totalSent * 100 / fileSize));
                reportProgress?.Invoke(progress);
            }
        }

        await stream.FlushAsync();
    }

    // 从网络流读取指定大小的文件内容并保存到本地。
    private static async Task ReceiveFileContentAsync(NetworkStream stream, string savePath, long fileSize, Action<int>? reportProgress)
    {
        byte[] buffer = new byte[FileBufferSize];
        long totalRead = 0;

        await using FileStream fileStream = new(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
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

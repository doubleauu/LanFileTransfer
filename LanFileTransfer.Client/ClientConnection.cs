// 封装客户端与服务端的基础通信操作，避免窗体代码直接处理网络流细节。
using System.Net.Sockets;
using LanFileTransfer.Common;

namespace LanFileTransfer.Client;

internal class ClientConnection
{
    private const int FileBufferSize = 64 * 1024;

    // 建立短连接，发送一条命令并接收服务端响应。
    public async Task<CommandMessage> SendCommandAsync(string serverIp, int port, MessageType messageType, params string[] fields)
    {
        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync(serverIp, port);

            using (NetworkStream stream = client.GetStream())
            {
                await TcpMessageProtocol.SendCommandAsync(stream, messageType, fields);
                return await TcpMessageProtocol.ReceiveCommandAsync(stream);
            }
        }
    }

    // 上传指定文件，先发送上传命令，再分块发送文件内容。
    public async Task<CommandMessage> UploadFileAsync(string serverIp, int port, UploadRequestDto request, string filePath, Action<int>? reportProgress)
    {
        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync(serverIp, port);

            using (NetworkStream stream = client.GetStream())
            {
                await TcpMessageProtocol.SendCommandAsync(
                    stream,
                    MessageType.UploadRequest,
                    request.UserId.ToString(),
                    request.ResourceName,
                    request.ResourceType.ToString(),
                    request.FileSize.ToString(),
                    request.OriginalFileName,
                    request.Extension,
                    request.FileHash ?? string.Empty);

                await SendFileContentAsync(stream, filePath, request.FileSize, reportProgress);
                return await TcpMessageProtocol.ReceiveCommandAsync(stream);
            }
        }
    }

    // 下载指定文件，先发送下载命令，再根据响应读取文件内容。
    public async Task<DownloadResponseDto?> DownloadFileAsync(string serverIp, int port, DownloadRequestDto request, string savePath, Action<int>? reportProgress)
    {
        using (TcpClient client = new TcpClient())
        {
            await client.ConnectAsync(serverIp, port);

            using (NetworkStream stream = client.GetStream())
            {
                await TcpMessageProtocol.SendCommandAsync(
                    stream,
                    MessageType.DownloadRequest,
                    request.UserId.ToString(),
                    request.FileId.ToString());

                CommandMessage responseMessage = await TcpMessageProtocol.ReceiveCommandAsync(stream);
                DownloadResponseDto? response = ParseDownloadResponse(responseMessage);
                if (response == null || !response.Success)
                {
                    return response;
                }

                await ReceiveFileContentAsync(stream, savePath, response.FileSize, reportProgress);
                return response;
            }
        }
    }

    // 将下载响应命令转换为下载响应对象。
    private static DownloadResponseDto? ParseDownloadResponse(CommandMessage message)
    {
        if (message.Type != MessageType.DownloadResponse)
        {
            return new DownloadResponseDto(false, "服务端返回的不是下载响应。", 0, string.Empty, ResourceType.File, 0);
        }

        bool success = string.Equals(message.GetField(0), "true", StringComparison.OrdinalIgnoreCase);
        string resultMessage = message.GetField(1);
        int.TryParse(message.GetField(2), out int fileId);
        Enum.TryParse(message.GetField(4), out ResourceType resourceType);
        long.TryParse(message.GetField(5), out long fileSize);

        return new DownloadResponseDto(
            success,
            resultMessage,
            fileId,
            message.GetField(3),
            resourceType,
            fileSize);
    }

    // 从本地文件读取字节并写入网络流，同时回调上传进度。
    private static async Task SendFileContentAsync(NetworkStream stream, string filePath, long fileSize, Action<int>? reportProgress)
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

                if (fileSize > 0)
                {
                    int progress = Math.Min(100, (int)(totalSent * 100 / fileSize));
                    reportProgress?.Invoke(progress);
                }
            }
        }

        await stream.FlushAsync();
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
}

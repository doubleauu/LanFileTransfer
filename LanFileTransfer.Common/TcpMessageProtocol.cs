// 封装 TCP 结构化消息收发，处理消息头长度、JSON 序列化和拆包读取。
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text.Json;

namespace LanFileTransfer.Common;

public static class TcpMessageProtocol
{
    private const int HeaderLengthBytes = 4;  // 消息头长度
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);  // 创建一个固定的json序列化规则

    public static async Task SendAsync<T>(NetworkStream stream, MessageType type, T body, CancellationToken cancellationToken = default)
    {
        byte[] bodyBytes = JsonSerializer.SerializeToUtf8Bytes(body, JsonOptions);  // 序列化
        ProtocolHeader header = new(type, bodyBytes.Length);
        byte[] headerBytes = JsonSerializer.SerializeToUtf8Bytes(header, JsonOptions);

        byte[] headerLengthBytes = new byte[HeaderLengthBytes];  // 用四字节写入消息头长度
        BinaryPrimitives.WriteInt32BigEndian(headerLengthBytes, headerBytes.Length);

        // 写入顺序：消息头长度、消息头内容、消息体内容。
        await stream.WriteAsync(headerLengthBytes, cancellationToken);
        await stream.WriteAsync(headerBytes, cancellationToken);
        await stream.WriteAsync(bodyBytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<ReceivedMessage> ReceiveAsync(NetworkStream stream, CancellationToken cancellationToken = default)
    {
        byte[] headerLengthBytes = await ReadExactAsync(stream, HeaderLengthBytes, cancellationToken);
        int headerLength = BinaryPrimitives.ReadInt32BigEndian(headerLengthBytes);

        if (headerLength <= 0)
        {
            throw new InvalidDataException("消息头长度不正确。");
        }

        byte[] headerBytes = await ReadExactAsync(stream, headerLength, cancellationToken);
        ProtocolHeader? header = JsonSerializer.Deserialize<ProtocolHeader>(headerBytes, JsonOptions);

        if (header == null || header.BodyLength < 0)
        {
            throw new InvalidDataException("消息头内容不正确。");
        }

        byte[] bodyBytes = await ReadExactAsync(stream, header.BodyLength, cancellationToken);
        return new ReceivedMessage(header.Type, bodyBytes);
    }

    private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int length, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[length];
        int offset = 0;

        // TCP 是字节流，这里循环读取直到拿满指定长度，避免拆包导致数据不完整。
        while (offset < length)
        {
            int readCount = await stream.ReadAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (readCount == 0)
            {
                throw new EndOfStreamException("连接已断开，消息读取不完整。");
            }

            offset += readCount;
        }

        return buffer;
    }

    private sealed record ProtocolHeader(MessageType Type, int BodyLength);
}

public sealed record ReceivedMessage(MessageType Type, byte[] BodyBytes)
{
    public T? ReadBody<T>()
    {
        // 业务处理时按消息类型反序列化为对应 DTO。
        return JsonSerializer.Deserialize<T>(BodyBytes, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

// 封装 TCP 命令字符串收发，处理命令长度、字段转义和拆包读取。
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace LanFileTransfer.Common
{
    public static class TcpMessageProtocol
    {
        private const int CommandLengthBytes = 4;  // 命令长度占 4 个字节
        private const char Separator = '|';        // 命令字段分隔符
        private const char EscapeChar = '\\';      // 字段转义符，是字符类型

        // 发送命令，格式为：4 字节命令长度 + UTF-8 命令文本。
        public static async Task SendCommandAsync(NetworkStream stream, MessageType type, params string[] fields)  // 自动打包
        {
            string commandText = BuildCommandText(type, fields);
            byte[] commandBytes = Encoding.UTF8.GetBytes(commandText);

            byte[] lengthBytes = BitConverter.GetBytes(commandBytes.Length);  // 先传命令长度

            await stream.WriteAsync(lengthBytes);
            await stream.WriteAsync(commandBytes);
            await stream.FlushAsync();  // 异步刷新缓冲区，强制把缓冲区内容发送出去
        }

        // 接收一条完整命令，先读长度，再按长度读取命令内容。
        public static async Task<CommandMessage> ReceiveCommandAsync(NetworkStream stream)
        {
            byte[] lengthBytes = await ReadExactAsync(stream, CommandLengthBytes);
            int commandLength = BitConverter.ToInt32(lengthBytes, 0);

            if (commandLength <= 0)
            {
                throw new InvalidDataException("命令长度不正确。");
            }

            byte[] commandBytes = await ReadExactAsync(stream, commandLength);
            string commandText = Encoding.UTF8.GetString(commandBytes);
            return ParseCommandText(commandText);
        }

        // 拼接整条命令文本，第一项是命令类型，后面是业务字段。
        private static string BuildCommandText(MessageType type, string[] fields)
        {
            List<string> parts = new List<string>();  // 创建一个空的字符串列表，动态大小
            parts.Add(type.ToString());

            foreach (string field in fields)
            {
                parts.Add(EscapeField(field));  // 拼接各段命令文本时先进行转义，防止解析错误
            }

            return string.Join(Separator, parts);  // 给列表中每一个字符串之间插入分隔符，并拼接成一个完整字符串返回
        }

        // 解析整条命令文本，把命令类型和字段拆开。
        private static CommandMessage ParseCommandText(string commandText)
        {
            List<string> parts = SplitCommand(commandText);
            if (parts.Count == 0 || !Enum.TryParse(parts[0], out MessageType type))
            {
                throw new InvalidDataException("未知命令类型。");
            }

            List<string> fields = new List<string>();  // 只存命令内容，排除类型
            for (int i = 1; i < parts.Count; i++)
            {
                fields.Add(parts[i]);
            }

            return new CommandMessage(type, fields);
        }

        // 转义逐个字段中的分隔符和换行，避免 Split 时切错字段。
        private static string EscapeField(string? field)
        {
            if (field == null)
            {
                return string.Empty;  // 返回一个空的字符串，长度为零
            }

            StringBuilder builder = new StringBuilder();  // （可变字符串缓冲区）比string类型拼接更高效
            foreach (char item in field)
            {
                if (item == EscapeChar || item == Separator)   // 转义分隔符
                {
                    builder.Append(EscapeChar);
                    builder.Append(item);
                }
                else if (item == '\n')  // 转义换行符
                {
                    builder.Append("\\n");
                }
                else if (item == '\r')  // 转移回车符
                {
                    builder.Append("\\r");
                }
                else
                {
                    builder.Append(item);
                }
            }

            return builder.ToString();
        }

        // 按分隔符拆分整条命令为字符串列表，同时还原被转义的字符。
        private static List<string> SplitCommand(string commandText)
        {
            List<string> fields = new List<string>();
            StringBuilder builder = new StringBuilder();  // 创建缓冲区
            bool escaping = false;  // 上一个是否特殊标记转义符

            foreach (char item in commandText)
            {
                if (escaping)
                {
                    if (item == 'n')
                    {
                        builder.Append('\n');
                    }
                    else if (item == 'r')
                    {
                        builder.Append('\r');
                    }
                    else
                    {
                        builder.Append(item);
                    }

                    escaping = false;
                }
                else if (item == EscapeChar)
                {
                    escaping = true;
                }
                else if (item == Separator)
                {
                    fields.Add(builder.ToString());
                    builder.Clear();
                }
                else
                {
                    builder.Append(item);
                }
            }

            fields.Add(builder.ToString());
            return fields;
        }

        // 从网络流中循环读取指定长度的数据，解决 TCP 拆包导致的一次读不完整问题。
        private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;  // 当前偏移量

            // TCP 是字节流，这里循环读取直到拿满指定长度。
            while (offset < length)
            {
                int readCount = await stream.ReadAsync(buffer.AsMemory(offset, length - offset));
                if (readCount == 0)
                {
                    throw new EndOfStreamException("连接已断开，命令读取不完整。");
                }

                offset += readCount;
            }

            return buffer;   // 返回读取到的字节流
        }
    }

    // 表示服务端接收到的一条命令，包含命令类型和字段列表。
    public class CommandMessage
    {
        public MessageType Type { get; set; }   //  命令类型
        public List<string> Fields { get; set; }  // 命令内容

        public CommandMessage(MessageType type, List<string> fields)
        {
            Type = type;
            Fields = fields;
        }

        // 按下标读取字段，超出范围时返回空字符串。
        public string GetField(int index)
        {
            if (index < 0 || index >= Fields.Count)
            {
                return string.Empty;
            }

            return Fields[index];
        }
    }
}

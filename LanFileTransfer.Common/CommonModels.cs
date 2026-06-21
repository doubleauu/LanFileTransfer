// 保存客户端和服务端共用的命令类型和资源类型。
namespace LanFileTransfer.Common
{
    // 消息类型用于区分客户端和服务端之间的业务命令。
    public enum MessageType
    {
        TestRequest,
        TestResponse,
        LoginRequest,
        LoginResponse,
        RegisterRequest,
        RegisterResponse,
        UploadRequest,
        UploadResponse,
        DownloadRequest,
        DownloadResponse,
        FileListRequest,
        FileListResponse,
        TransferRecordRequest,
        TransferRecordResponse,
        ErrorResponse
    }

    // 资源类型用于区分普通文件和文件夹压缩包。
    public enum ResourceType
    {
        File,
        FolderZip   // 文件夹传输使用压缩包，操作简单还方便保留文件夹内部目录
    }

    // 传输类型用于保存上传或下载历史。
    public enum TransferType
    {
        Upload,
        Download
    }

    // 传输状态用于记录传输是否成功。
    public enum TransferStatus
    {
        Success
    }
}

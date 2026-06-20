// 保存客户端和服务端共用的命令类型、资源类型和简单数据对象。
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

    // 资源类型用于区分普通文件、文件夹压缩包和多文件压缩包。
    public enum ResourceType
    {
        File,
        FolderZip,   // 保留文件夹内部目录
        MultiFileZip  // 只是多个文件整合一起方便传输，不需要关注内部目录
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
        Success,
        Failed
    }

    // 登录请求对象。
    public class LoginRequestDto
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public LoginRequestDto(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

    // 登录响应对象。
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }

        public LoginResponseDto(bool success, string message, int? userId, string? username)
        {
            Success = success;
            Message = message;
            UserId = userId;
            Username = username;
        }
    }

    // 注册请求对象。
    public class RegisterRequestDto
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public RegisterRequestDto(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

    // 注册响应对象。
    public class RegisterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? UserId { get; set; }

        public RegisterResponseDto(bool success, string message, int? userId)
        {
            Success = success;
            Message = message;
            UserId = userId;
        }
    }

    // 上传请求对象，文件内容会在命令后继续通过 NetworkStream 分块传输。
    public class UploadRequestDto
    {
        public int UserId { get; set; }
        public string ResourceName { get; set; }
        public ResourceType ResourceType { get; set; }
        public long FileSize { get; set; }
        public string OriginalFileName { get; set; }
        public string Extension { get; set; }
        public string? FileHash { get; set; }

        public UploadRequestDto(int userId, string resourceName, ResourceType resourceType, long fileSize, string originalFileName, string extension, string? fileHash)
        {
            UserId = userId;
            ResourceName = resourceName;
            ResourceType = resourceType;
            FileSize = fileSize;
            OriginalFileName = originalFileName;
            Extension = extension;
            FileHash = fileHash;
        }
    }

    // 上传响应对象。
    public class UploadResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? FileId { get; set; }
        public long BytesTransferred { get; set; }

        public UploadResponseDto(bool success, string message, int? fileId, long bytesTransferred)
        {
            Success = success;
            Message = message;
            FileId = fileId;
            BytesTransferred = bytesTransferred;
        }
    }

    // 下载请求对象。
    public class DownloadRequestDto
    {
        public int UserId { get; set; }
        public int FileId { get; set; }

        public DownloadRequestDto(int userId, int fileId)
        {
            UserId = userId;
            FileId = fileId;
        }
    }

    // 下载响应对象。
    public class DownloadResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int FileId { get; set; }
        public string OriginalFileName { get; set; }
        public ResourceType ResourceType { get; set; }
        public long FileSize { get; set; }

        public DownloadResponseDto(bool success, string message, int fileId, string originalFileName, ResourceType resourceType, long fileSize)
        {
            Success = success;
            Message = message;
            FileId = fileId;
            OriginalFileName = originalFileName;
            ResourceType = resourceType;
            FileSize = fileSize;
        }
    }

    // 文件列表请求对象。
    public class FileListRequestDto
    {
        public int UserId { get; set; }

        public FileListRequestDto(int userId)
        {
            UserId = userId;
        }
    }

    // 文件列表中的单行文件信息。
    public class FileListItemDto
    {
        public int FileId { get; set; }
        public string OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public ResourceType ResourceType { get; set; }
        public string UploaderName { get; set; }
        public DateTime UploadedAt { get; set; }

        public FileListItemDto(int fileId, string originalFileName, long fileSize, ResourceType resourceType, string uploaderName, DateTime uploadedAt)
        {
            FileId = fileId;
            OriginalFileName = originalFileName;
            FileSize = fileSize;
            ResourceType = resourceType;
            UploaderName = uploaderName;
            UploadedAt = uploadedAt;
        }
    }

    // 文件列表响应对象。
    public class FileListResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<FileListItemDto> Files { get; set; }

        public FileListResponseDto(bool success, string message, List<FileListItemDto> files)
        {
            Success = success;
            Message = message;
            Files = files;
        }
    }

    // 传输记录请求对象，后续展示历史记录时使用。
    public class TransferRecordRequestDto
    {
        public int UserId { get; set; }

        public TransferRecordRequestDto(int userId)
        {
            UserId = userId;
        }
    }

    // 传输记录中的单行信息，后续展示历史记录时使用。
    public class TransferRecordItemDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int FileId { get; set; }
        public TransferType TransferType { get; set; }
        public TransferStatus Status { get; set; }
        public long BytesTransferred { get; set; }
        public string ClientIp { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }

        public TransferRecordItemDto(int id, int userId, int fileId, TransferType transferType, TransferStatus status, long bytesTransferred, string clientIp, DateTime startedAt, DateTime finishedAt)
        {
            Id = id;
            UserId = userId;
            FileId = fileId;
            TransferType = transferType;
            Status = status;
            BytesTransferred = bytesTransferred;
            ClientIp = clientIp;
            StartedAt = startedAt;
            FinishedAt = finishedAt;
        }
    }

    // 传输记录响应对象，后续展示历史记录时使用。
    public class TransferRecordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<TransferRecordItemDto> Records { get; set; }

        public TransferRecordResponseDto(bool success, string message, List<TransferRecordItemDto> records)
        {
            Success = success;
            Message = message;
            Records = records;
        }
    }

    // 连接测试使用的简单消息体。
    public class TestMessageDto
    {
        public string Content { get; set; }

        public TestMessageDto(string content)
        {
            Content = content;
        }
    }

    // 通用错误响应对象。
    public class ErrorResponseDto
    {
        public string Message { get; set; }
        public string? Detail { get; set; }

        public ErrorResponseDto(string message, string? detail)
        {
            Message = message;
            Detail = detail;
        }
    }
}

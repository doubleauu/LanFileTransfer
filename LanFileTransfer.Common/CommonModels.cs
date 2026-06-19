// 保存客户端和服务端共用的消息类型、资源类型和数据传输对象。
namespace LanFileTransfer.Common;

// 消息类型用于区分客户端和服务端之间的业务请求或响应。
public enum MessageType
{
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

// 传输类型和状态用于保存上传、下载历史。
public enum TransferType
{
    Upload,
    Download
}

// 传输状态：
public enum TransferStatus
{
    Success,
    Failed
}

// 登录和注册相关 DTO。
public sealed record LoginRequestDto(string Username, string Password);

public sealed record LoginResponseDto(
    bool Success,
    string Message,
    int? UserId,
    string? Username);

public sealed record RegisterRequestDto(string Username, string Password);

public sealed record RegisterResponseDto(
    bool Success,
    string Message,
    int? UserId);

// 上传和下载相关 DTO；文件内容后续会通过 NetworkStream 分块传输。
public sealed record UploadRequestDto(
    int UserId,
    string ResourceName,
    ResourceType ResourceType,
    long FileSize,
    string OriginalFileName,
    string Extension,
    string? FileHash);

public sealed record UploadResponseDto(
    bool Success,
    string Message,
    int? FileId,
    long BytesTransferred);

public sealed record DownloadRequestDto(
    int UserId,
    int FileId);

public sealed record DownloadResponseDto(
    bool Success,
    string Message,
    int FileId,
    string OriginalFileName,
    ResourceType ResourceType,
    long FileSize);

// 文件列表相关 DTO，用于客户端表格展示。
public sealed record FileListRequestDto(int UserId);

public sealed record FileListItemDto(
    int FileId,
    string OriginalFileName,
    long FileSize,
    ResourceType ResourceType,
    string UploaderName,
    DateTime UploadedAt);

public sealed record FileListResponseDto(
    bool Success,
    string Message,
    IReadOnlyList<FileListItemDto> Files);

// 传输记录相关 DTO，用于展示上传和下载历史。
public sealed record TransferRecordRequestDto(int UserId);

public sealed record TransferRecordItemDto(
    int Id,
    int UserId,
    int FileId,
    TransferType TransferType,
    TransferStatus Status,
    long BytesTransferred,
    string ClientIp,
    DateTime StartedAt,
    DateTime FinishedAt);

public sealed record TransferRecordResponseDto(
    bool Success,
    string Message,
    IReadOnlyList<TransferRecordItemDto> Records);

// 通用响应和错误响应，适合简单结果提示。
public sealed record ResponseDto(
    bool Success,
    string Message);

public sealed record ResponseDto<T>(
    bool Success,
    string Message,
    T? Data);  // 泛型适应不同的返回类型，可能是登录结果，文件传输结果等

public sealed record ErrorResponseDto(
    string Message,
    string? Detail);

# 局域网文件传输系统

这是一个 C# 网络应用编程课程大作业项目，实现了基于 C/S 架构的局域网文件传输系统。客户端使用 WinForms，服务端使用 Console，通信使用 `TcpClient`、`TcpListener` 和 `NetworkStream`，数据保存使用 EF Core + SQLite。

## 一、主要功能

- 用户注册和登录
- 客户端连接服务端
- 单文件上传和下载
- 文件夹自动压缩为 ZIP 后上传
- 文件夹 ZIP 下载后可选择解压
- 文件列表查询
- 上传和下载进度显示
- 上传、下载传输记录查询
- **服务端**保存文件元数据和传输记录

## 二、项目结构

```text
LanFileTransfer
├── LanFileTransfer.Client      WinForms 客户端
├── LanFileTransfer.Server      Console 服务端
├── LanFileTransfer.Common      客户端和服务端共用枚举与 TCP 命令协议
├── LanFileTransfer.Data        EF Core 数据库上下文和实体类
└── LanFileTransfer.sln         Visual Studio 解决方案文件
```

### 1. LanFileTransfer.Client

客户端项目，主要负责图形界面和用户操作。

主要文件：

- `Program.cs`：WinForms 客户端入口。
- `Form1.cs`：主窗体界面、按钮事件、文件选择、上传下载操作。
- `ClientConnection.cs`：封装客户端 TCP 连接、命令发送、文件分块上传和下载。

### 2. LanFileTransfer.Server

服务端项目，主要负责监听客户端连接、处理请求、保存文件和访问数据库。

主要文件：

- `Program.cs`：服务端入口，启动 TCP 监听，默认端口为 `5000`。
- `ClientHandler.cs`：处理登录、注册、上传、下载、文件列表、传输记录等业务命令。
- `ServerDatabase.cs`：创建 SQLite 数据库连接，并在服务端启动时初始化数据库。

### 3. LanFileTransfer.Common

公共类库，客户端和服务端都会引用。

主要文件：

- `CommonModels.cs`：保存消息类型、资源类型和传输记录相关枚举。
- `TcpMessageProtocol.cs`：封装 TCP 命令字符串协议。

通信格式采用教材风格的普通命令字符串，例如：

```text
LoginRequest|用户名|密码
RegisterRequest|用户名|密码
FileListRequest|用户Id
UploadRequest|用户Id|资源类型|文件大小|原文件名|扩展名
DownloadRequest|用户Id|文件Id
TransferRecordRequest|用户Id
```

为了避免 TCP 粘包和拆包问题，命令发送时使用：

```text
4 字节命令长度 + UTF-8 命令字符串
```

字段之间使用 `|` 分隔，协议类会对字段中的 `|`、`\` 和换行做简单转义。文件内容不放在命令字符串中，而是在上传或下载命令后通过 `NetworkStream` 分块传输。

### 4. LanFileTransfer.Data

数据访问类库，主要负责 EF Core 和 SQLite 数据库实体。

主要文件：

- `LanFileTransferDbContext.cs`：包含数据库上下文和三个实体类。

数据库实体：

- `User`：保存用户账号和密码哈希。
- `FileRecord`：保存文件名、大小、保存路径、资源类型等文件元数据。
- `TransferRecord`：保存上传和下载记录。

当前项目为了贴近课程作业规模，没有再单独保留一套 DTO 对象。客户端和服务端直接根据命令字段读写业务数据，代码更直观，也方便课堂展示。

## 三、运行环境

建议环境：

- Windows
- Visual Studio 2022
- .NET 8 SDK
- 支持 WinForms 的桌面开发环境

## 四、运行方法

1. 使用 VS 2022 打开 `LanFileTransfer.sln`。
2. 先启动 `LanFileTransfer.Server` 项目。（服务端只需要一台电脑启动即可）
3. 再启动 `LanFileTransfer.Client` 项目。
4. 客户端服务器 IP 填写服务端电脑的局域网 IP，端口填写 `5000`。
   如果客户端和服务端在同一台电脑上自测，填写 `127.0.0.1`。
5. 点击“连接测试”，成功后即可注册、登录、上传和下载文件。

## 五、使用流程

1. 启动服务端，等待控制台显示监听端口。
2. 启动客户端，输入服务端电脑的局域网 IP 和端口。
3. 点击“连接测试”确认网络连接正常。
4. 输入用户名和密码，点击“注册”创建账号。
5. 点击“登录”进入系统。
6. 点击“上传文件”上传单个文件。
7. 点击“上传文件夹”选择文件夹，客户端会自动压缩为 ZIP 后上传。
8. 点击“刷新列表”查看服务端已有文件。
9. 在表格中选择文件，点击“下载文件”保存到本地。
10. 如果下载的是文件夹 ZIP，客户端会询问是否解压。
11. 点击“传输记录”查看当前用户的上传和下载历史。

## 六、数据和文件保存位置

服务端启动后会在运行目录生成：

- `LanFileTransfer.db`：SQLite 数据库文件。
- `ServerStorage`：服务端保存上传文件的目录。

其中：

- 数据库保存用户、文件信息和传输记录。
- 实际文件保存在服务端本地磁盘中。
- 上传文件先写入临时目录，校验大小后再移动到正式保存目录。（避免留下不完整文件）

## 七、课程知识点对应

本项目主要使用了课程中的以下知识点：

- WinForms：窗体、按钮、文本框、表格、文件选择对话框。
- TCP 编程：`TcpClient`、`TcpListener`、`NetworkStream`。
- TCP 字节流处理：使用长度前缀解决粘包和拆包问题。
- 简单文本协议：使用 `命令类型|字段1|字段2` 的方式表达业务请求。
- 文件流：使用 `FileStream` 分块读写文件。
- 文件夹处理：使用 ZIP 压缩和解压文件夹。
- EF Core：使用 DbContext、实体类和 LINQ 查询 SQLite 数据库。
- 异步编程：使用 `async` 和 `await` 避免界面卡死。

## 八、注意事项

- 服务端必须先启动，客户端才能连接成功。
- 客户端和服务端端口必须一致，默认端口是 `5000`。
- 两台电脑测试时，需要把客户端 IP 改成服务端电脑的局域网 IP。
- 如果 Windows 防火墙拦截服务端端口，需要允许服务端程序通过防火墙。
- `LanFileTransfer.db` 和上传文件属于运行生成内容，不需要手动创建。

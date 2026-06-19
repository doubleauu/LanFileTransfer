// 客户端主窗体，提供服务器连接测试和操作日志显示。
using LanFileTransfer.Common;

namespace LanFileTransfer.Client;

public partial class Form1 : Form
{
    private readonly ClientConnection clientConnection = new();
    private readonly TextBox txtServerIp = new();
    private readonly TextBox txtPort = new();
    private readonly TextBox txtUsername = new();
    private readonly TextBox txtPassword = new();
    private readonly Button btnConnect = new();
    private readonly Button btnLogin = new();
    private readonly Button btnRegister = new();
    private readonly Button btnUploadFile = new();
    private readonly Button btnRefreshFiles = new();
    private readonly Button btnDownloadFile = new();
    private readonly Label lblStatus = new();
    private readonly ProgressBar progressTransfer = new();  // 进度条
    private readonly TextBox txtLog = new();
    private readonly DataGridView gridFiles = new();
    private int? currentUserId;

    // 初始化窗体并创建客户端界面控件。
    public Form1()
    {
        InitializeComponent();
        BuildConnectionUi();
    }

    // 构建连接、登录、上传和日志显示相关控件。
    private void BuildConnectionUi()
    {
        // 上方是服务器连接配置，后续所有请求都会使用这组 IP 和端口。
        Label lblIp = new()
        {
            AutoSize = true,
            Location = new Point(24, 28),
            Text = "服务器 IP"
        };

        txtServerIp.Location = new Point(100, 24);
        txtServerIp.Size = new Size(160, 25);
        txtServerIp.Text = "127.0.0.1";

        Label lblPort = new()
        {
            AutoSize = true,
            Location = new Point(280, 28),
            Text = "端口"
        };

        txtPort.Location = new Point(325, 24);
        txtPort.Size = new Size(80, 25);
        txtPort.Text = "5000";

        btnConnect.Location = new Point(425, 22);
        btnConnect.Size = new Size(120, 30);
        btnConnect.Text = "连接测试";
        btnConnect.Click += BtnConnect_Click;

        Label lblUsername = new()
        {
            AutoSize = true,
            Location = new Point(24, 76),
            Text = "用户名"
        };

        txtUsername.Location = new Point(100, 72);
        txtUsername.Size = new Size(160, 25);

        Label lblPassword = new()
        {
            AutoSize = true,
            Location = new Point(280, 76),
            Text = "密码"
        };

        txtPassword.Location = new Point(325, 72);
        txtPassword.Size = new Size(120, 25);
        txtPassword.UseSystemPasswordChar = true;

        btnLogin.Location = new Point(465, 70);
        btnLogin.Size = new Size(80, 30);
        btnLogin.Text = "登录";
        btnLogin.Click += BtnLogin_Click;

        btnRegister.Location = new Point(465, 108);
        btnRegister.Size = new Size(80, 30);
        btnRegister.Text = "注册";
        btnRegister.Click += BtnRegister_Click;

        btnUploadFile.Location = new Point(465, 146);
        btnUploadFile.Size = new Size(80, 30);
        btnUploadFile.Text = "上传文件";
        btnUploadFile.Click += BtnUploadFile_Click;

        btnRefreshFiles.Location = new Point(560, 146);
        btnRefreshFiles.Size = new Size(90, 30);
        btnRefreshFiles.Text = "刷新列表";
        btnRefreshFiles.Click += BtnRefreshFiles_Click;

        btnDownloadFile.Location = new Point(660, 146);
        btnDownloadFile.Size = new Size(80, 30);
        btnDownloadFile.Text = "下载文件";
        btnDownloadFile.Click += BtnDownloadFile_Click;

        lblStatus.AutoSize = true;
        lblStatus.Location = new Point(24, 120);
        lblStatus.Text = "状态：未连接";

        progressTransfer.Location = new Point(24, 148);
        progressTransfer.Size = new Size(420, 24);
        progressTransfer.Minimum = 0;
        progressTransfer.Maximum = 100;

        txtLog.Location = new Point(24, 188);
        txtLog.Size = new Size(700, 120);
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;

        gridFiles.Location = new Point(24, 320);
        gridFiles.Size = new Size(700, 240);
        gridFiles.AllowUserToAddRows = false;
        gridFiles.AllowUserToDeleteRows = false;
        gridFiles.ReadOnly = true;
        gridFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridFiles.RowHeadersVisible = false;
        BuildFileGridColumns();

        Controls.AddRange(new Control[]  // 将上面的全部控件添加到窗体中
        {
            lblIp,
            txtServerIp,
            lblPort,
            txtPort,
            btnConnect,
            lblUsername,
            txtUsername,
            lblPassword,
            txtPassword,
            btnLogin,
            btnRegister,
            btnUploadFile,
            btnRefreshFiles,
            btnDownloadFile,
            lblStatus,
            progressTransfer,
            txtLog,
            gridFiles
        });
    }

    // 初始化文件列表表格字段。
    private void BuildFileGridColumns()
    {
        gridFiles.Columns.Add("FileId", "文件 ID");
        gridFiles.Columns.Add("OriginalFileName", "文件名");
        gridFiles.Columns.Add("FileSize", "大小");
        gridFiles.Columns.Add("ResourceType", "类型");
        gridFiles.Columns.Add("UploaderName", "上传者");
        gridFiles.Columns.Add("UploadedAt", "上传时间");
    }

    // 处理连接测试按钮点击，发送测试消息检查服务端是否可用。
    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (!TryGetServer(out string serverIp, out int port))
            {
                return;
            }

            lblStatus.Text = "状态：正在连接";
            await SendTestMessageAsync(serverIp, port);
        });
    }

    // 处理登录按钮点击，将用户名和密码发送给服务端验证。
    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (!TryGetServer(out string serverIp, out int port) || !TryGetUserInput(out string username, out string password))
            {
                return;
            }

            LoginRequestDto request = new(username, password);
            AppendLog($"正在连接 {serverIp}:{port} ...");
            ReceivedMessage response = await clientConnection.SendRequestAsync(serverIp, port, MessageType.LoginRequest, request);
            AppendLog($"已发送：{MessageType.LoginRequest}");
            ShowLoginResponse(response);
        });
    }

    // 处理注册按钮点击，将新用户信息发送给服务端保存。
    private async void BtnRegister_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (!TryGetServer(out string serverIp, out int port) || !TryGetUserInput(out string username, out string password))
            {
                return;
            }

            RegisterRequestDto request = new(username, password);
            AppendLog($"正在连接 {serverIp}:{port} ...");
            ReceivedMessage response = await clientConnection.SendRequestAsync(serverIp, port, MessageType.RegisterRequest, request);
            AppendLog($"已发送：{MessageType.RegisterRequest}");
            ShowRegisterResponse(response);
        });
    }

    // 处理上传文件按钮点击，弹出文件目录窗口，选择本地文件后开始上传。
    private async void BtnUploadFile_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (currentUserId == null)
            {
                AppendLog("请先登录后再上传文件。");
                return;
            }

            if (!TryGetServer(out string serverIp, out int port))
            {
                return;
            }

            using OpenFileDialog dialog = new()
            {
                Title = "选择要上传的文件"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await UploadFileAsync(serverIp, port, dialog.FileName);
        });
    }

    // 处理刷新列表按钮点击，从服务端查询已上传文件。
    private async void BtnRefreshFiles_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (currentUserId == null)
            {
                AppendLog("请先登录后再刷新文件列表。");
                return;
            }

            if (!TryGetServer(out string serverIp, out int port))
            {
                return;
            }

            FileListRequestDto request = new(currentUserId.Value);
            ReceivedMessage response = await clientConnection.SendRequestAsync(serverIp, port, MessageType.FileListRequest, request);
            ShowFileListResponse(response);
        });
    }

    // 处理下载文件按钮点击，下载表格中当前选中的文件。
    private async void BtnDownloadFile_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (currentUserId == null)
            {
                AppendLog("请先登录后再下载文件。");
                return;
            }

            if (!TryGetServer(out string serverIp, out int port) || !TryGetSelectedFile(out int fileId, out string fileName))
            {
                return;
            }

            using SaveFileDialog dialog = new()
            {
                Title = "保存下载文件",
                FileName = fileName
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await DownloadFileAsync(serverIp, port, fileId, dialog.FileName);
        });
    }

    // 发送连接测试消息并显示服务端响应。
    private async Task SendTestMessageAsync(string serverIp, int port)
    {
        try
        {
            // 结构化消息，登录、上传、下载都会复用这套协议。
            string testMessage = $"客户端连接测试 {DateTime.Now:HH:mm:ss}";
            AppendLog($"正在连接 {serverIp}:{port} ...");
            ReceivedMessage response = await clientConnection.SendRequestAsync(serverIp, port, MessageType.TestRequest, new TestMessageDto(testMessage));
            AppendLog($"已发送：{MessageType.TestRequest}");
            ShowServerResponse(response);  // 服务端返回接受状态并输出
        }
        catch (Exception ex)
        {
            lblStatus.Text = "状态：连接失败";
            AppendLog($"连接失败：{ex.Message}");
        }
    }

    // 上传指定文件，先发送元数据，再分块发送文件内容。
    private async Task UploadFileAsync(string serverIp, int port, string filePath)
    {
        FileInfo fileInfo = new(filePath);
        UploadRequestDto request = new(
            currentUserId!.Value,  // 感叹号表示忽略之前的的可空约束，保证非空
            fileInfo.Name,
            ResourceType.File,
            fileInfo.Length,
            fileInfo.Name,
            fileInfo.Extension,
            null);

        try
        {
            AppendLog($"开始上传：{fileInfo.Name}");
            progressTransfer.Value = 0;

            // 文件内容不放进 JSON，而是在上传请求后由通信类直接按块写入网络流。
            ReceivedMessage response = await clientConnection.UploadFileAsync(serverIp, port, request, filePath, progress =>
            {
                progressTransfer.Value = progress;
            });
            ShowUploadResponse(response);

            if (response.ReadBody<UploadResponseDto>()?.Success == true)
            {
                await RefreshFileListAfterUploadAsync(serverIp, port);
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "状态：上传失败";
            AppendLog($"上传失败：{ex.Message}");
        }
    }

    private async Task RefreshFileListAfterUploadAsync(string serverIp, int port)
    {
        if (currentUserId == null)
        {
            return;
        }

        FileListRequestDto request = new(currentUserId.Value);
        ReceivedMessage response = await clientConnection.SendRequestAsync(serverIp, port, MessageType.FileListRequest, request);
        ShowFileListResponse(response);
    }


    // 输出 服务端返回状态
    private void ShowServerResponse(ReceivedMessage response)
    {
        if (response.Type == MessageType.TestResponse)
        {
            TestMessageDto? body = response.ReadBody<TestMessageDto>();
            lblStatus.Text = "状态：连接测试成功";
            AppendLog($"服务端响应：{body?.Content}");
            return;
        }

        ErrorResponseDto? error = response.ReadBody<ErrorResponseDto>();
        lblStatus.Text = "状态：服务端返回错误";
        AppendLog($"服务端错误：{error?.Message}");
    }

    // 输出登录状态到日志区
    private void ShowLoginResponse(ReceivedMessage response)
    {
        LoginResponseDto? body = response.ReadBody<LoginResponseDto>();
        lblStatus.Text = body?.Success == true ? $"状态：已登录 {body.Username}" : "状态：登录失败";
        AppendLog($"登录结果：{body?.Message}");

        if (body?.Success == true)
        {
            currentUserId = body.UserId;
        }
    }

    // 输出注册状态到日志区
    private void ShowRegisterResponse(ReceivedMessage response)
    {
        RegisterResponseDto? body = response.ReadBody<RegisterResponseDto>();
        lblStatus.Text = body?.Success == true ? "状态：注册成功" : "状态：注册失败";
        AppendLog($"注册结果：{body?.Message}");
    }

    // 输出上传结果到状态栏和日志区。
    private void ShowUploadResponse(ReceivedMessage response)
    {
        UploadResponseDto? body = response.ReadBody<UploadResponseDto>();
        lblStatus.Text = body?.Success == true ? "状态：上传成功" : "状态：上传失败";
        AppendLog($"上传结果：{body?.Message}，已传输 {body?.BytesTransferred ?? 0} 字节");

        if (body?.Success == true)
        {
            progressTransfer.Value = 100;
        }
    }

    private async Task DownloadFileAsync(string serverIp, int port, int fileId, string savePath)
    {
        try
        {
            progressTransfer.Value = 0;
            DownloadRequestDto request = new(currentUserId!.Value, fileId);
            DownloadResponseDto? response = await clientConnection.DownloadFileAsync(serverIp, port, request, savePath, progress =>
            {
                progressTransfer.Value = progress;
            });

            if (response?.Success == true)
            {
                progressTransfer.Value = 100;
                lblStatus.Text = "状态：下载成功";
                AppendLog($"下载成功：{savePath}");
            }
            else
            {
                lblStatus.Text = "状态：下载失败";
                AppendLog($"下载失败：{response?.Message}");
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = "状态：下载失败";
            AppendLog($"下载失败：{ex.Message}");
        }
    }

    // 将服务端返回的文件列表填充到表格。
    private void ShowFileListResponse(ReceivedMessage response)
    {
        FileListResponseDto? body = response.ReadBody<FileListResponseDto>();
        if (body?.Success != true)
        {
            lblStatus.Text = "状态：刷新列表失败";
            AppendLog($"文件列表：{body?.Message}");
            return;
        }

        gridFiles.Rows.Clear();
        foreach (FileListItemDto file in body.Files)
        {
            gridFiles.Rows.Add(
                file.FileId,
                file.OriginalFileName,
                FormatFileSize(file.FileSize),
                file.ResourceType,
                file.UploaderName,
                file.UploadedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        lblStatus.Text = "状态：文件列表已刷新";
        AppendLog($"文件列表：{body.Message}，共 {body.Files.Count} 个文件");
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }

        return $"{bytes / 1024.0 / 1024.0:F1} MB";
    }

    private bool TryGetSelectedFile(out int fileId, out string fileName)
    {
        fileId = 0;
        fileName = string.Empty;

        if (gridFiles.CurrentRow == null)
        {
            AppendLog("请先在文件列表中选择一个文件。");
            return false;
        }

        object? fileIdValue = gridFiles.CurrentRow.Cells["FileId"].Value;
        object? fileNameValue = gridFiles.CurrentRow.Cells["OriginalFileName"].Value;

        if (fileIdValue == null || !int.TryParse(fileIdValue.ToString(), out fileId) || fileNameValue == null)
        {
            AppendLog("选中文件信息不完整。");
            return false;
        }

        fileName = fileNameValue.ToString() ?? "download.dat";
        return true;
    }

    // 参数是：返回值同时声明
    // 获得服务端ip和端口号
    private bool TryGetServer(out string serverIp, out int port)
    {
        serverIp = txtServerIp.Text.Trim();
        port = 0;

        if (string.IsNullOrWhiteSpace(serverIp))
        {
            AppendLog("服务器 IP 不能为空。");
            return false;
        }

        if (!int.TryParse(txtPort.Text.Trim(), out port))
        {
            AppendLog("端口格式不正确。");   // 输出到界面日志内
            return false;
        }

        return true;
    }

    // 获取用户信息
    private bool TryGetUserInput(out string username, out string password)
    {
        username = txtUsername.Text.Trim();
        password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            AppendLog("用户名和密码不能为空。");
            return false;
        }

        return true;
    }

    // 定义异步执行任务，期间禁用按钮
    private async Task RunWithButtonsDisabledAsync(Func<Task> action)
    {
        btnConnect.Enabled = false;
        btnLogin.Enabled = false;
        btnRegister.Enabled = false;
        btnUploadFile.Enabled = false;
        btnRefreshFiles.Enabled = false;
        btnDownloadFile.Enabled = false;

        try
        {
            await action();
        }
        finally
        {
            btnConnect.Enabled = true;
            btnLogin.Enabled = true;
            btnRegister.Enabled = true;
            btnUploadFile.Enabled = true;
            btnRefreshFiles.Enabled = true;
            btnDownloadFile.Enabled = true;
        }
    }

    // 向日志文本框追加一条带时间的消息。
    private void AppendLog(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}

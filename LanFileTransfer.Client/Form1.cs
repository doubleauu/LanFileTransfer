// 客户端主窗体，提供服务器连接测试和操作日志显示。
using System.IO.Compression;
using LanFileTransfer.Common;

namespace LanFileTransfer.Client;

public partial class Form1 : Form
{
    // redonly 表示只读字段
    private readonly ClientConnection clientConnection = new();
    private readonly TextBox txtServerIp = new();
    private readonly TextBox txtPort = new();
    private readonly TextBox txtUsername = new();
    private readonly TextBox txtPassword = new();
    private readonly Button btnConnect = new();
    private readonly Button btnLogin = new();
    private readonly Button btnRegister = new();
    private readonly Button btnUploadFile = new();
    private readonly Button btnUploadFolder = new();
    private readonly Button btnRefreshFiles = new();
    private readonly Button btnDownloadFile = new();
    private readonly Button btnTransferRecords = new();
    private readonly Label lblCurrentUser = new();
    private readonly Label lblProgress = new();
    private readonly ProgressBar progressTransfer = new();  // 进度条
    private readonly TextBox txtLog = new();
    private readonly DataGridView gridFiles = new();
    private int? currentUserId;
    private bool showingTransferRecords;

    // 初始化窗体并创建客户端界面控件。
    public Form1()
    {
        InitializeComponent();
        BuildConnectionUi();
    }

    // 构建连接、登录、上传和日志显示相关控件。
    private void BuildConnectionUi()
    {
        GroupBox groupConnection = new GroupBox
        {
            Location = new Point(24, 16),
            Size = new Size(840, 72),
            Text = "服务器连接"
        };

        GroupBox groupUser = new GroupBox
        {
            Location = new Point(24, 98),
            Size = new Size(840, 78),
            Text = "用户信息"
        };

        GroupBox groupActions = new GroupBox
        {
            Location = new Point(24, 186),
            Size = new Size(840, 76),
            Text = "文件操作"
        };

        // 上方是服务器连接配置，后续所有请求都会使用这组 IP 和端口。
        Label lblIp = new()
        {
            AutoSize = true,
            Location = new Point(18, 31),
            Text = "服务器 IP"
        };

        txtServerIp.Location = new Point(94, 27);
        txtServerIp.Size = new Size(160, 25);
        txtServerIp.Text = "127.0.0.1";

        Label lblPort = new()
        {
            AutoSize = true,
            Location = new Point(280, 31),
            Text = "端口"
        };

        txtPort.Location = new Point(325, 27);
        txtPort.Size = new Size(80, 25);
        txtPort.Text = "5000";

        btnConnect.Location = new Point(430, 25);
        btnConnect.Size = new Size(120, 30);
        btnConnect.Text = "连接测试";
        btnConnect.Click += BtnConnect_Click;

        Label lblUsername = new()
        {
            AutoSize = true,
            Location = new Point(18, 33),
            Text = "用户名"
        };

        txtUsername.Location = new Point(94, 29);
        txtUsername.Size = new Size(160, 25);

        Label lblPassword = new()
        {
            AutoSize = true,
            Location = new Point(280, 33),
            Text = "密码"
        };

        txtPassword.Location = new Point(325, 29);
        txtPassword.Size = new Size(120, 25);
        txtPassword.UseSystemPasswordChar = true;

        btnLogin.Location = new Point(468, 27);
        btnLogin.Size = new Size(80, 30);
        btnLogin.Text = "登录";
        btnLogin.Click += BtnLogin_Click;

        btnRegister.Location = new Point(562, 27);
        btnRegister.Size = new Size(80, 30);
        btnRegister.Text = "注册";
        btnRegister.Click += BtnRegister_Click;

        lblCurrentUser.AutoSize = true;
        lblCurrentUser.Location = new Point(668, 33);
        lblCurrentUser.Text = "当前用户：未登录";

        btnUploadFile.Location = new Point(18, 28);
        btnUploadFile.Size = new Size(90, 30);
        btnUploadFile.Text = "上传文件";
        btnUploadFile.Click += BtnUploadFile_Click;

        btnUploadFolder.Location = new Point(124, 28);
        btnUploadFolder.Size = new Size(100, 30);
        btnUploadFolder.Text = "上传文件夹";
        btnUploadFolder.Click += BtnUploadFolder_Click;

        btnRefreshFiles.Location = new Point(240, 28);
        btnRefreshFiles.Size = new Size(90, 30);
        btnRefreshFiles.Text = "刷新列表";
        btnRefreshFiles.Click += BtnRefreshFiles_Click;

        btnDownloadFile.Location = new Point(346, 28);
        btnDownloadFile.Size = new Size(90, 30);
        btnDownloadFile.Text = "下载文件";
        btnDownloadFile.Click += BtnDownloadFile_Click;

        btnTransferRecords.Location = new Point(452, 28);
        btnTransferRecords.Size = new Size(90, 30);
        btnTransferRecords.Text = "传输记录";
        btnTransferRecords.Click += BtnTransferRecords_Click;

        lblProgress.AutoSize = true;
        lblProgress.Location = new Point(24, 276);
        lblProgress.Text = "进度：";

        progressTransfer.Location = new Point(70, 272);
        progressTransfer.Size = new Size(794, 24);
        progressTransfer.Minimum = 0;
        progressTransfer.Maximum = 100;

        txtLog.Location = new Point(24, 586);
        txtLog.Size = new Size(840, 72);
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;

        gridFiles.Location = new Point(24, 312);
        gridFiles.Size = new Size(840, 260);
        gridFiles.AllowUserToAddRows = false;
        gridFiles.AllowUserToDeleteRows = false;
        gridFiles.ReadOnly = true;
        gridFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        gridFiles.RowHeadersVisible = false;  // 默认不可视
        BuildFileGridColumns();

        groupConnection.Controls.AddRange(new Control[]
        {
            lblIp,
            txtServerIp,
            lblPort,
            txtPort,
            btnConnect
        });

        groupUser.Controls.AddRange(new Control[]
        {
            lblUsername,
            txtUsername,
            lblPassword,
            txtPassword,
            btnLogin,
            btnRegister,
            lblCurrentUser
        });

        groupActions.Controls.AddRange(new Control[]
        {
            btnUploadFile,
            btnUploadFolder,
            btnRefreshFiles,
            btnDownloadFile,
            btnTransferRecords
        });

        Controls.AddRange(new Control[]  // 将上面的全部控件添加到窗体中
        {
            groupConnection,
            groupUser,
            groupActions,
            lblProgress,
            progressTransfer,
            txtLog,
            gridFiles
        });
    }

    // 初始化文件列表表格字段。
    private void BuildFileGridColumns()
    {
        gridFiles.Columns.Clear();
        gridFiles.Columns.Add("FileId", "文件 ID");
        gridFiles.Columns.Add("OriginalFileName", "文件名");
        gridFiles.Columns.Add("FileSize", "大小");
        gridFiles.Columns.Add("ResourceType", "类型");
        gridFiles.Columns.Add("UploaderName", "上传者");
        gridFiles.Columns.Add("UploadedAt", "上传时间");
    }

    // 初始化传输记录表格字段。
    private void BuildTransferRecordGridColumns()
    {
        gridFiles.Columns.Clear();
        gridFiles.Columns.Add("Id", "记录 ID");
        gridFiles.Columns.Add("FileId", "文件 ID");
        gridFiles.Columns.Add("TransferType", "类型");
        gridFiles.Columns.Add("Status", "状态");
        gridFiles.Columns.Add("BytesTransferred", "字节数");
        gridFiles.Columns.Add("ClientIp", "客户端");
        gridFiles.Columns.Add("StartedAt", "开始时间");
        gridFiles.Columns.Add("FinishedAt", "结束时间");
    }

    // 处理连接测试按钮点击，发送测试消息检查服务端是否可用。
    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        // 异步lambda表达式，传入一个异步执行任务，async 表示该任务可以使用 await
        await RunWithButtonsDisabledAsync(async () =>    
        {
            if (!TryGetServer(out string serverIp, out int port))
            {
                return;
            }

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

            AppendLog($"正在连接 {serverIp}:{port} ...");
            CommandMessage response = await clientConnection.SendCommandAsync(serverIp, port, MessageType.LoginRequest, username, password);
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

            AppendLog($"正在连接 {serverIp}:{port} ...");
            CommandMessage response = await clientConnection.SendCommandAsync(serverIp, port, MessageType.RegisterRequest, username, password);
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

            // 创建一个文件选择窗口
            using OpenFileDialog dialog = new()
            {
                Title = "选择要上传的文件"
            };

            // 以模态窗口打开，用户没有确认则直接退出
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            // 第三个参数为完整文件路径
            await UploadFileAsync(serverIp, port, dialog.FileName);
        });
    }

    // 处理上传文件夹按钮点击，先压缩文件夹，再按 ZIP 文件上传。
    private async void BtnUploadFolder_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (currentUserId == null)
            {
                AppendLog("请先登录后再上传文件夹。");
                return;
            }

            if (!TryGetServer(out string serverIp, out int port))
            {
                return;
            }

            using FolderBrowserDialog dialog = new()
            {
                Description = "选择要上传的文件夹",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await UploadFolderAsync(serverIp, port, dialog.SelectedPath);
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

            CommandMessage response = await clientConnection.SendCommandAsync(serverIp, port, MessageType.FileListRequest, currentUserId.Value.ToString());
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

            if (!TryGetServer(out string serverIp, out int port) || !TryGetSelectedFile(out int fileId, out string fileName, out ResourceType resourceType))
            {
                return;
            }

            using SaveFileDialog dialog = new()
            {
                Title = "保存下载文件",
                FileName = GetDownloadFileName(fileName, resourceType)
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await DownloadFileAsync(serverIp, port, fileId, dialog.FileName);
        });
    }

    // 处理传输记录按钮点击，从服务端查询当前用户的上传和下载记录。
    private async void BtnTransferRecords_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (currentUserId == null)
            {
                AppendLog("请先登录后再查看传输记录。");
                return;
            }

            if (!TryGetServer(out string serverIp, out int port))
            {
                return;
            }

            CommandMessage response = await clientConnection.SendCommandAsync(serverIp, port, MessageType.TransferRecordRequest, currentUserId.Value.ToString());
            ShowTransferRecordResponse(response);
        });
    }

    // 发送连接测试消息并显示服务端响应。
    private async Task SendTestMessageAsync(string serverIp, int port)
    {
        try
        {
            // 命令字符串协议，登录、上传、下载都会复用这套格式。
            string testMessage = $"客户端连接测试 {DateTime.Now:HH:mm:ss}";
            AppendLog($"正在连接 {serverIp}:{port} ...");
            CommandMessage response = await clientConnection.SendCommandAsync(serverIp, port, MessageType.TestRequest, testMessage);
            AppendLog($"已发送：{MessageType.TestRequest}");
            ShowServerResponse(response);  // 客户端接受服务端返回状态并输出
        }
        catch (Exception ex)
        {
            AppendLog($"连接失败：{ex.Message}");
        }
    }

    // 上传指定文件，先发送元数据，再分块发送文件内容。
    private async Task UploadFileAsync(string serverIp, int port, string filePath)
    {
        FileInfo fileInfo = new(filePath);
            await UploadResourceAsync(
                serverIp,
                port,
                filePath,
                fileInfo.Name,
                fileInfo.Extension,
                ResourceType.File,
            deleteAfterUpload: false);
    }

    // 压缩文件夹为临时 ZIP，并复用普通上传流程。
    private async Task UploadFolderAsync(string serverIp, int port, string folderPath)
    {
        string folderName = new DirectoryInfo(folderPath).Name;
        // 创建临时缓存文件夹，combine用来拼接路径
        string tempDirectory = Path.Combine(Path.GetTempPath(), "LanFileTransfer");
        Directory.CreateDirectory(tempDirectory);
        // 使用GUID标识，防止文件名重复
        string zipPath = Path.Combine(tempDirectory, $"{folderName}_{Guid.NewGuid():N}.zip");
        try
        {
            AppendLog($"正在压缩文件夹：{folderName}");
            // 压缩文件夹：
            ZipFile.CreateFromDirectory(folderPath, zipPath, CompressionLevel.Fastest, includeBaseDirectory: true);

            await UploadResourceAsync(
                serverIp,
                port,
                zipPath,
                $"{folderName}.zip",
                ".zip",
                ResourceType.FolderZip,
                deleteAfterUpload: true);
        }
        catch (Exception ex)
        {
            AppendLog($"文件夹上传失败：{ex.Message}");
            DeleteFileIfExists(zipPath);
        }
    }

    // 上传文件或文件夹 ZIP，区别由 resourceType 记录到数据库中。
    private async Task UploadResourceAsync(
        string serverIp,
        int port,
        string filePath,
        string originalFileName,
        string extension,
        ResourceType resourceType,
        bool deleteAfterUpload)
    {
        FileInfo fileInfo = new(filePath);

        try
        {
            AppendLog($"开始上传：{originalFileName}");
            progressTransfer.Value = 0;

            // 文件内容不放进命令字符串，而是在上传命令后由通信类直接按块写入网络流。
            CommandMessage response = await clientConnection.UploadFileAsync(
                serverIp,
                port,
                currentUserId!.Value,
                resourceType,
                fileInfo.Length,
                originalFileName,
                extension,
                filePath,
                progress => progressTransfer.Value = progress);
            ShowUploadResponse(response);

            if (response.Type == MessageType.UploadResponse && IsProtocolTrue(response.GetField(0)))
            {
                await RefreshFileListAfterUploadAsync(serverIp, port);
            }
        }
        catch (Exception ex)
        {
            AppendLog($"上传失败：{ex.Message}");
        }
        finally
        {
            if (deleteAfterUpload)
            {
                DeleteFileIfExists(filePath);
            }
        }
    }

    // 上传成功后自动刷新文件列表。
    private async Task RefreshFileListAfterUploadAsync(string serverIp, int port)
    {
        if (currentUserId == null)
        {
            return;
        }

        CommandMessage response = await clientConnection.SendCommandAsync(serverIp, port, MessageType.FileListRequest, currentUserId.Value.ToString());
        ShowFileListResponse(response);
    }


    // 输出 服务端返回状态
    private void ShowServerResponse(CommandMessage response)
    {
        if (response.Type == MessageType.TestResponse)
        {
            AppendLog($"服务端响应：{response.GetField(0)}");
            return;
        }

        AppendLog($"服务端错误：{GetResponseMessage(response)}");
    }

    // 输出登录状态到日志区
    private void ShowLoginResponse(CommandMessage response)
    {
        AppendLog($"登录结果：{GetResponseMessage(response)}");

        if (response.Type == MessageType.LoginResponse && IsProtocolTrue(response.GetField(0)))
        {
            if (int.TryParse(response.GetField(2), out int userId))
            {
                currentUserId = userId;
            }

            lblCurrentUser.Text = $"当前用户：{response.GetField(3)}";
        }
    }

    // 输出注册状态到日志区
    private void ShowRegisterResponse(CommandMessage response)
    {
        AppendLog($"注册结果：{GetResponseMessage(response)}");
    }

    // 输出上传结果到状态栏和日志区。
    private void ShowUploadResponse(CommandMessage response)
    {
        long.TryParse(response.GetField(3), out long bytesTransferred);
        AppendLog($"上传结果：{GetResponseMessage(response)}，已传输 {bytesTransferred} 字节");

        if (response.Type == MessageType.UploadResponse && IsProtocolTrue(response.GetField(0)))
        {
            progressTransfer.Value = 100;
        }
    }

    // 下载选中的文件，若下载的是文件夹 ZIP，则提供解压选项。
    private async Task DownloadFileAsync(string serverIp, int port, int fileId, string savePath)
    {
        try
        {
            progressTransfer.Value = 0;
            CommandMessage response = await clientConnection.DownloadFileAsync(serverIp, port, currentUserId!.Value, fileId, savePath, progress =>
            {
                progressTransfer.Value = progress;
            });

            if (response.Type == MessageType.DownloadResponse && IsProtocolTrue(response.GetField(0)))
            {
                progressTransfer.Value = 100;
                AppendLog($"下载成功：{savePath}");

                Enum.TryParse(response.GetField(4), out ResourceType resourceType);
                if (resourceType == ResourceType.FolderZip)
                {
                    ExtractFolderZipIfNeeded(savePath);
                }
            }
            else
            {
                AppendLog($"下载失败：{GetResponseMessage(response)}");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"下载失败：{ex.Message}");
        }
    }

    // 文件夹 ZIP 下载完成后询问是否解压。
    private void ExtractFolderZipIfNeeded(string zipPath)
    {
        DialogResult result = MessageBox.Show(this, "文件夹 ZIP 已下载，是否现在解压？", "解压文件夹", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
        {
            return;  // 不解压直接退出
        }

        using FolderBrowserDialog dialog = new FolderBrowserDialog
        {
            Description = "选择解压保存位置",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            // 创建压缩包目录：用户选择目录 + 不带拓展名的文件夹名称
            string extractDirectory = Path.Combine(dialog.SelectedPath, Path.GetFileNameWithoutExtension(zipPath));
            Directory.CreateDirectory(extractDirectory);
            // 目标解压目录，如果存在就覆盖
            ZipFile.ExtractToDirectory(zipPath, extractDirectory, overwriteFiles: true);
            AppendLog($"解压成功：{extractDirectory}");
        }
        catch (Exception ex)
        {
            AppendLog($"解压失败：{ex.Message}");
        }
    }

    // 将服务端返回的文件列表填充到表格。
    private void ShowFileListResponse(CommandMessage response)
    {
        if (response.Type != MessageType.FileListResponse || !IsProtocolTrue(response.GetField(0)))
        {
            AppendLog($"文件列表：{GetResponseMessage(response)}");
            return;
        }

        gridFiles.Rows.Clear();
        if (showingTransferRecords)
        {
            BuildFileGridColumns();
            showingTransferRecords = false;
        }

        int.TryParse(response.GetField(2), out int fileCount);
        int index = 3;
        for (int i = 0; i < fileCount && index + 5 < response.Fields.Count; i++)
        {
            int.TryParse(response.GetField(index), out int fileId);
            string originalFileName = response.GetField(index + 1);
            long.TryParse(response.GetField(index + 2), out long fileSize);
            Enum.TryParse(response.GetField(index + 3), out ResourceType resourceType);
            string uploaderName = response.GetField(index + 4);
            DateTime.TryParse(response.GetField(index + 5), out DateTime uploadedAt);

            gridFiles.Rows.Add(
                fileId,
                originalFileName,
                FormatFileSize(fileSize),
                resourceType,
                uploaderName,
                uploadedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            index += 6;
        }

        AppendLog($"文件列表：{response.GetField(1)}，共 {fileCount} 个文件");
    }

    // 将服务端返回的传输记录填充到表格。
    private void ShowTransferRecordResponse(CommandMessage response)
    {
        if (response.Type != MessageType.TransferRecordResponse || !IsProtocolTrue(response.GetField(0)))
        {
            AppendLog($"传输记录：{GetResponseMessage(response)}");
            return;
        }

        if (!showingTransferRecords)
        {
            BuildTransferRecordGridColumns();
            showingTransferRecords = true;
        }

        gridFiles.Rows.Clear();
        int.TryParse(response.GetField(2), out int recordCount);
        int index = 3;
        for (int i = 0; i < recordCount && index + 8 < response.Fields.Count; i++)
        {
            int.TryParse(response.GetField(index), out int id);
            int.TryParse(response.GetField(index + 2), out int fileId);
            Enum.TryParse(response.GetField(index + 3), out TransferType transferType);
            Enum.TryParse(response.GetField(index + 4), out TransferStatus status);
            long.TryParse(response.GetField(index + 5), out long bytesTransferred);
            string clientIp = response.GetField(index + 6);
            DateTime.TryParse(response.GetField(index + 7), out DateTime startedAt);
            DateTime.TryParse(response.GetField(index + 8), out DateTime finishedAt);

            gridFiles.Rows.Add(
                id,
                fileId,
                transferType,
                status,
                bytesTransferred,
                clientIp,
                startedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                finishedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            index += 9;
        }

        AppendLog($"传输记录：{response.GetField(1)}，共 {recordCount} 条记录");
    }

    // 判断协议中的布尔文本是否表示 true。
    private static bool IsProtocolTrue(string value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    // 读取服务端响应中的提示信息。
    private static string GetResponseMessage(CommandMessage response)
    {
        if (response.Fields.Count > 1)
        {
            return response.GetField(1);
        }

        if (response.Fields.Count == 1)
        {
            return response.GetField(0);
        }

        return "未知响应。";
    }

    // 将字节数格式化为适合界面显示的大小。
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

    // 删除临时文件，主要用于上传文件夹后清理临时 ZIP。
    private static void DeleteFileIfExists(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    // 根据资源类型生成默认下载文件名。
    private static string GetDownloadFileName(string fileName, ResourceType resourceType)
    {
        if (resourceType == ResourceType.FolderZip && !fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return $"{fileName}.zip";
        }

        return fileName;
    }

    // 尝试从表格当前行读取文件 ID、文件名和资源类型。
    private bool TryGetSelectedFile(out int fileId, out string fileName, out ResourceType resourceType)
    {
        fileId = 0;
        fileName = string.Empty;
        resourceType = ResourceType.File;

        if (gridFiles.CurrentRow == null)
        {
            AppendLog("请先在文件列表中选择一个文件。");
            return false;
        }

        if (showingTransferRecords)
        {
            AppendLog("当前显示的是传输记录，请先刷新文件列表后再下载。");
            return false;
        }

        object? fileIdValue = gridFiles.CurrentRow.Cells["FileId"].Value;
        object? fileNameValue = gridFiles.CurrentRow.Cells["OriginalFileName"].Value;
        object? resourceTypeValue = gridFiles.CurrentRow.Cells["ResourceType"].Value;

        if (fileIdValue == null || !int.TryParse(fileIdValue.ToString(), out fileId) || fileNameValue == null || resourceTypeValue == null)
        {
            AppendLog("选中文件信息不完整。");
            return false;
        }

        fileName = fileNameValue.ToString() ?? "download.dat";
        Enum.TryParse(resourceTypeValue.ToString(), out resourceType);
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

    // 定义异步等待执行任务，期间禁用其他按钮
    private async Task RunWithButtonsDisabledAsync(Func<Task> action)
    {
        btnConnect.Enabled = false;
        btnLogin.Enabled = false;
        btnRegister.Enabled = false;
        btnUploadFile.Enabled = false;
        btnUploadFolder.Enabled = false;
        btnRefreshFiles.Enabled = false;
        btnDownloadFile.Enabled = false;
        btnTransferRecords.Enabled = false;

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
            btnUploadFolder.Enabled = true;
            btnRefreshFiles.Enabled = true;
            btnDownloadFile.Enabled = true;
            btnTransferRecords.Enabled = true;
        }
    }

    // 向日志文本框追加一条带时间的消息。
    private void AppendLog(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}

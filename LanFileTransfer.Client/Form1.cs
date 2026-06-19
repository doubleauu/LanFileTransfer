// 客户端主窗体，提供服务器连接测试和操作日志显示。
using System.Net.Sockets;
using LanFileTransfer.Common;

namespace LanFileTransfer.Client;

public partial class Form1 : Form
{
    private readonly TextBox txtServerIp = new();
    private readonly TextBox txtPort = new();
    private readonly TextBox txtUsername = new();
    private readonly TextBox txtPassword = new();
    private readonly Button btnConnect = new();
    private readonly Button btnLogin = new();
    private readonly Button btnRegister = new();
    private readonly Label lblStatus = new();
    private readonly TextBox txtLog = new();

    public Form1()
    {
        InitializeComponent();
        BuildConnectionUi();
    }

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

        lblStatus.AutoSize = true;
        lblStatus.Location = new Point(24, 120);
        lblStatus.Text = "状态：未连接";

        txtLog.Location = new Point(24, 155);
        txtLog.Size = new Size(550, 205);
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;

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
            lblStatus,
            txtLog
        });
    }

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

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (!TryGetServer(out string serverIp, out int port) || !TryGetUserInput(out string username, out string password))
            {
                return;
            }

            LoginRequestDto request = new(username, password);
            ReceivedMessage response = await SendRequestAsync(serverIp, port, MessageType.LoginRequest, request);
            ShowLoginResponse(response);
        });
    }

    private async void BtnRegister_Click(object? sender, EventArgs e)
    {
        await RunWithButtonsDisabledAsync(async () =>
        {
            if (!TryGetServer(out string serverIp, out int port) || !TryGetUserInput(out string username, out string password))
            {
                return;
            }

            RegisterRequestDto request = new(username, password);
            ReceivedMessage response = await SendRequestAsync(serverIp, port, MessageType.RegisterRequest, request);
            ShowRegisterResponse(response);
        });
    }

    private async Task SendTestMessageAsync(string serverIp, int port)
    {
        try
        {
            // 阶段四改为结构化消息，后续登录、上传、下载都会复用这套协议。
            string testMessage = $"客户端连接测试 {DateTime.Now:HH:mm:ss}";
            ReceivedMessage response = await SendRequestAsync(serverIp, port, MessageType.TestRequest, new TestMessageDto(testMessage));
            ShowServerResponse(response);
        }
        catch (Exception ex)
        {
            lblStatus.Text = "状态：连接失败";
            AppendLog($"连接失败：{ex.Message}");
        }
    }

    private async Task<ReceivedMessage> SendRequestAsync<T>(string serverIp, int port, MessageType messageType, T request)
    {
        AppendLog($"正在连接 {serverIp}:{port} ...");

        // 每次请求建立一个短连接，当前阶段逻辑最简单，后续需要持续连接时再调整。
        using TcpClient client = new();
        await client.ConnectAsync(serverIp, port);

        await using NetworkStream stream = client.GetStream();
        await TcpMessageProtocol.SendAsync(stream, messageType, request);
        AppendLog($"已发送：{messageType}");

        return await TcpMessageProtocol.ReceiveAsync(stream);
    }


    // 输出服务端返回状态
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
    }

    // 输出注册状态到日志区
    private void ShowRegisterResponse(ReceivedMessage response)
    {
        RegisterResponseDto? body = response.ReadBody<RegisterResponseDto>();
        lblStatus.Text = body?.Success == true ? "状态：注册成功" : "状态：注册失败";
        AppendLog($"注册结果：{body?.Message}");
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

    // 异步执行，期间禁用按钮
    private async Task RunWithButtonsDisabledAsync(Func<Task> action)
    {
        btnConnect.Enabled = false;
        btnLogin.Enabled = false;
        btnRegister.Enabled = false;

        try
        {
            await action();
        }
        finally
        {
            btnConnect.Enabled = true;
            btnLogin.Enabled = true;
            btnRegister.Enabled = true;
        }
    }

    private void AppendLog(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}

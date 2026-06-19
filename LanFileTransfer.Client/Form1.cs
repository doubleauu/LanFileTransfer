// 客户端主窗体，提供服务器连接测试和操作日志显示。
using System.Net.Sockets;
using System.Text;

namespace LanFileTransfer.Client;

public partial class Form1 : Form
{
    private readonly TextBox txtServerIp = new();
    private readonly TextBox txtPort = new();
    private readonly Button btnConnect = new();
    private readonly Label lblStatus = new();
    private readonly TextBox txtLog = new();

    public Form1()
    {
        InitializeComponent();
        BuildConnectionUi();
    }

    private void BuildConnectionUi()
    {
        // 阶段三先完成连接测试界面，后续再扩展登录、上传、下载区域。
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

        lblStatus.AutoSize = true;
        lblStatus.Location = new Point(24, 72);
        lblStatus.Text = "状态：未连接";

        txtLog.Location = new Point(24, 105);
        txtLog.Size = new Size(550, 250);
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
            lblStatus,
            txtLog
        });
    }

    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (!int.TryParse(txtPort.Text.Trim(), out int port))
        {
            AppendLog("端口格式不正确。");   // 输出到界面日志内
            return;
        }

        btnConnect.Enabled = false;
        lblStatus.Text = "状态：正在连接";

        try
        {
            await SendTestMessageAsync(txtServerIp.Text.Trim(), port);
        }
        finally
        {
            btnConnect.Enabled = true;
        }
    }

    private async Task SendTestMessageAsync(string serverIp, int port)
    {
        try
        {
            AppendLog($"正在连接 {serverIp}:{port} ...");

            using TcpClient client = new();
            await client.ConnectAsync(serverIp, port);

            await using NetworkStream stream = client.GetStream();
            await using StreamWriter writer = new(stream, Encoding.UTF8, leaveOpen: true)
            {
                AutoFlush = true
            };
            using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);

            // 阶段三只发送一条测试文本，确认 TCP 基础收发正常。
            string testMessage = $"客户端连接测试 {DateTime.Now:HH:mm:ss}";
            await writer.WriteLineAsync(testMessage);
            AppendLog($"已发送：{testMessage}");

            string? response = await reader.ReadLineAsync();
            lblStatus.Text = "状态：连接测试成功";
            AppendLog($"服务端响应：{response}");
        }
        catch (Exception ex)
        {
            lblStatus.Text = "状态：连接失败";
            AppendLog($"连接失败：{ex.Message}");
        }
    }

    private void AppendLog(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}

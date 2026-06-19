// 客户端程序入口，负责启动 WinForms 主窗体。
namespace LanFileTransfer.Client;

internal static class Program
{
    [STAThread]  // 指定程序在“单线程单元 STA 模式”运行
    private static void Main()
    {
        // 初始化 WinForms 默认配置后打开客户端主窗体。
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}

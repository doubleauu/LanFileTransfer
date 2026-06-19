// 客户端主窗体的基础设计器代码，保存控件字段和初始化入口。
namespace LanFileTransfer.Client;

partial class Form1
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        // 释放窗体创建的组件资源。
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(620, 420);
        MinimumSize = new Size(620, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "局域网文件传输系统 - 客户端";
    }
}

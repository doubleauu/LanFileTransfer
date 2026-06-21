# C# 网络应用编程课件初学者总结版

> 说明：本资料整理自 `C:\桌面\c#ppt` 下 13 份 PDF 课件。目标是给初学者提供一份“不太精简”的学习版总结：不仅列考点，也解释概念、API、常见流程、易错点和典型应用。

## 0. 学习路线

1. 先建立 C#、.NET、Visual Studio、项目和解决方案的整体概念。
2. 再掌握控制台和 WinForms，这是后面聊天程序、数据库界面、网络客户端的基础。
3. 学文件、流、数据库、LINQ 和 EF Core，因为网络程序经常要读写文件、传输字节、展示表格数据。
4. 学 IP、端口、DNS、进程和线程，为 TCP/UDP 编程补齐运行基础。
5. 学 TCP：先理解 TcpClient、TcpListener、NetworkStream 和消息边界，再看同步、异步、聊天、游戏和协同绘图项目。
6. 学 UDP：重点比较 TCP/UDP，理解 UdpClient、广播、组播和会议程序。

---

## 1. C#、.NET 与 Visual Studio 基础

### 学习目标

- 能解释 C#、.NET、Visual Studio IDE、Visual Studio Code 的关系。
- 能区分项目（Project）和解决方案（Solution），知道源码备份不能只备份子文件夹。
- 能理解命名空间（Namespace）、类（Class）、Main 方法、顶级语句和全局 using 的作用。
- 能知道常见网络应用模型：C/S、B/S、P2P。

### 核心概念

- C#（C Sharp）是 .NET 平台的首选语言，是完全面向对象的程序设计语言；.NET 是免费、跨平台、开源的开发平台。
- Visual Studio IDE 是集成度高的“重量级”开发工具，适合完整项目；Visual Studio Code 更轻量，依靠扩展支持多语言。
- VS2022 可开发 WinForms、WPF、TCP/UDP/HTTP 网络应用、ASP.NET Core Web 应用等。
- 项目用于组织源代码和资源；解决方案用于组织多个项目。课程示例会同时创建客户端控制台、客户端 WinForms、服务端控制台、Web 应用等项目。
- 命名空间是逻辑划分，不是磁盘目录本身；它用于把类分组，避免类名冲突。
- Main 方法是应用程序入口点，方法名固定为 `Main`，返回类型通常为 `void` 或 `int`。
- 顶级语句会让编译器自动合成 Program 类和 Main 方法；全局 using 会在整个应用程序范围内引入命名空间。

### 关键 API / 类 / 方法

- `namespace`：组织类，类似逻辑目录。
- `Main`：程序入口点；控制台和 WinForms 都从入口方法开始执行。
- `using` / 全局 `using`：引入命名空间，减少完整类型名书写。
- 断点调试（Breakpoint Debugging）：通过暂停点逐行观察变量变化、程序路径和逻辑错误。

### 代码模式或流程

```csharp
namespace ClientConsoleExamples.ch01;

internal class HelloWorld
{
    public static void Run()
    {
        Console.WriteLine("Hello World");
    }
}
```

- 新建项目后要确认启动项目；解决方案里有多个项目时，按 F5 跑哪个项目由启动项目决定。
- 备份源码时应压缩解决方案所在顶级文件夹，不能只备份子文件夹，否则项目文件、资源或解决方案文件可能丢失。
- 命名约定：类名、方法名和属性名使用 Pascal 命名法，例如 `HelloWorld`、`GetData`；变量名、对象名和方法参数使用 Camel 命名法，例如 `userName`、`userAge`。

### 初学者易错点

- 把命名空间当成物理文件夹。命名空间是逻辑组织方式，文件夹只是项目管理方式。
- 忘记设置启动项目，导致 F5 运行的不是自己刚写的程序。
- 只复制 `.cs` 文件，不复制 `.csproj`、`.sln`、资源文件和配置文件，换电脑后项目无法恢复。
- 不用断点，只靠肉眼看代码。复杂逻辑尤其需要断点调试。

### 典型应用场景

- 企业桌面应用：WinForms、WPF、C#。
- 网络应用：TCP、UDP、HTTP 等协议类程序。
- Web 应用：ASP.NET Core、JavaScript、HTML、CSS、C#。
- C/S 适合本地客户端连接服务器；B/S 适合浏览器访问；P2P 适合点对点文件分发等场景。

### 复习检查清单

- 我能说出 C# 与 .NET 的关系吗？
- 我知道 VS IDE 和 VS Code 的区别吗？
- 我能解释项目、解决方案、命名空间、类的层次吗？
- 我能创建控制台项目和 WinForms 项目，并设置启动项目吗？

---

## 2. 控制台与 WinForms 入门

### 学习目标

- 掌握控制台输出、输入和格式化字符串。
- 掌握 WinForms 程序入口、窗体显示方式、控件属性、消息框与事件注册。
- 熟悉常用控件：Label、LinkLabel、Button、TextBox、Panel、GroupBox、RadioButton、CheckBox、ListBox、ComboBox、PictureBox、ImageList。

### 核心概念

- 控制台（Console）程序通过命令行窗口进行文本输入输出，资源占用少，适合作为入门练习和服务端示例。
- `Console.Write` 输出后不换行；`Console.WriteLine` 输出后自动换行。
- `Console.ReadLine` 从标准输入读取到回车为止，返回字符串；`Console.ReadKey` 读取单个按键，返回 `ConsoleKeyInfo`。
- 格式化字符串可以用占位符、`String.Format`、`Console.WriteLine` 参数序列和 `$"..."` 字符串插值实现。
- WinForms 程序入口在 `Program.cs`，通常通过 `Application.Run(new MainForm())` 启动[[消息循环]]并显示窗体。
- `Show` 显示非模式窗体，不阻止用户操作其他窗体；`ShowDialog` 显示模式窗体，关闭后才继续执行后续代码。
- 控件和代码隐藏类相关联：拖放控件、修改属性、注册事件都会影响窗体对应的代码。

### 关键 API / 类 / 方法

- `Console.Write` / `Console.WriteLine`：控制台输出。
- `Console.ReadLine` / `Console.ReadKey`：控制台输入。
- `String.Format` 与字符串插值 `$"{x}"`：格式化数据。
- `Application.Run` / `Application.Exit`：启动与退出 WinForms 应用。
- `Form.Show` / `Form.ShowDialog` / `Form.Hide` / `Form.Close`：窗体显示、隐藏、关闭。
- `MessageBox.Show`：显示模式提示框，返回 `DialogResult`。
- `Anchor` / `Dock`：控制窗体大小变化时控件的位置和占据方式。

### 代码模式或流程

#### 2.1 控制台输入、处理、输出

```csharp
Console.Write("请输入两个数（空格分隔）：");
var input = Console.ReadLine();
if (input == null) return;

string[] parts = input.Split(' ');
int a = int.Parse(parts[0]);
int b = int.Parse(parts[1]);
Console.WriteLine($"两个数的和为：{a + b}");
```

- 这个流程来自课件例 2-1：提示用户输入、读取字符串、判空、按空格拆分、转换为整数、输出计算结果。
- 初学时建议把 `int.Parse` 换成 `int.TryParse` 做输入校验，避免用户输入非数字时程序崩溃。[[TryParse方法]]

#### 2.2 格式化输出

```csharp
int x = 10, y = 20, z = 30;
Console.WriteLine("{0}+{1}+{2}={3}", x, y, z, x + y + z);
Console.WriteLine($"{x}+{y}+{z}={x + y + z}");
```

- `{参数序号[, M][:格式码]}` 中 `M` 表示最小宽度，正数右对齐、负数左对齐；格式码用于控制数值、日期等格式。
- 初学阶段优先使用字符串插值，因为它更直观；看到老代码中的 `{0}`、`String.Format` 时要能读懂。

#### 2.3 先显示欢迎窗体，再显示主窗体

```csharp
[STAThread]
static void Main()
{
    ApplicationConfiguration.Initialize();
    WelcomeForm form = new WelcomeForm();
    form.ShowDialog();
    Application.Run(new MainForm());
}
```

- `ShowDialog` 让欢迎窗体以[[模态方式]]方式显示，关闭后才进入主窗体。
- 登录窗体也常用这种模式：先验证用户，验证成功再显示主界面。

#### 2.4 RadioButton 事件复用
- 单选按钮控件
```csharp
private void radioButton_CheckedChanged(object sender, EventArgs e)
{
    string s = "选项为：";
    s += GetSelectedItem(groupBox1);
    s += GetSelectedItem(groupBox2);
    labelMessage.Text = s.TrimEnd('，');
}

private static string GetSelectedItem(GroupBox groupbox)
{
    string s = "";
    foreach (Control c in groupbox.Controls)
    {
        if (c is RadioButton r && r.Checked)
            s += r.Text + "，";
    }
    return s;
}
```
[[事件函数参数理解]]
- 一组 `RadioButton` 通常放在同一个 `GroupBox` 中实现组内互斥。
- 多个控件可以注册到同一个事件处理方法，避免每个按钮都写重复代码。

#### 2.5 ListBox / ComboBox 常见操作
- 二者都是显示多个选项的控件
- `ListBox` 是列表框控件，直接显示多个选项，用户直接选择
- `ComboBox` 是下拉选择框，用户可以选择，也可以手动输入
```csharp
string[] items = { "C#", "数据库", "网络编程" };  // 创建字符串数组
comboBox1.Items.AddRange(items);
listBox1.SelectionMode = SelectionMode.MultiExtended;  // 设置ListBox支持多选，可以按 `Ctrl` 选择多个不连续项，可以按 `Shift` 选择连续多项

if (!comboBox1.Items.Contains(comboBox1.Text))
{
    comboBox1.Items.Add(comboBox1.Text);
    listBox1.Items.Add(comboBox1.Text);
}
```

- `Items.Add`、`Items.AddRange`、`Items.Clear`、`Items.Contains`、`Items.Remove` 是 ListBox 和 ComboBox 的高频操作。[[选项框常用函数]]
- `SelectedIndex` 从 0 开始；`SelectedItem` 获取当前项；`SelectedItems` 获取多选集合。

### 初学者易错点

- `Console.ReadLine()` 可能返回 `null`，直接 `Split` 会导致空引用异常。
- `Show` 和 `ShowDialog` 混用会改变程序执行顺序；登录、欢迎、文件选择这类**必须先处理**完的窗体通常适合 `ShowDialog`。
- 控件点击没响应时，先检查事件是否注册，而不是只看控件有没有放到窗体上。
- 在不同 `GroupBox` 里放 `RadioButton` 才能形成多个互斥组。
- `ComboBox.DropDownStyle` 不同会影响用户能否编辑文本：`DropDownList` 只能选，不能手输。
- `PictureBox.SizeMode` 影响图片显示：`StretchImage` 会变形，`Zoom` 保持比例，`Normal` 可能裁剪。

### 典型应用场景

- 控制台适合作为网络服务端、命令行工具、入门练习程序。
- WinForms 适合做可视化客户端，例如登录界面、聊天客户端、数据库管理界面、绘图客户端。
- PictureBox / ImageList 适合图像显示、按钮图标、简单图片资源管理。

### 复习检查清单

- 我能写一个读取两个数并输出和的控制台程序吗？
- 我能解释 `Write` 和 `WriteLine` 的区别吗？
- 我知道 `Show` 与 `ShowDialog` 的区别吗？
- 我能给 Button、RadioButton、CheckBox 注册事件吗？

---

## 3. 文件读写与数据流

### 学习目标

- 理解文件编码（Encoding）和文本文件读写的关系。
- 掌握 `File` 类读写、追加文本的方法，以及 OpenFileDialog / SaveFileDialog 的作用。
- 理解 FileStream、MemoryStream、NetworkStream、StreamReader、StreamWriter 的职责。
- 理解序列化（Serialization）和反序列化（Deserialization）的用途，尤其是网络传输和持久化存储。

### 核心概念

- 文件是按某种形式保存在磁盘或光盘上的一系列数据；保存文本时必须考虑编码和解码，否则中文可能乱码。一般使用 `UTF-8`。
- `File.WriteAllText` / `File.WriteAllLines` 适合一次性写入文本；`File.ReadAllText` / `File.ReadAllLines` 适合一次性读取文本。
- `File.AppendAllText` 适合在文件末尾追加内容。
- `OpenFileDialog` 用于让用户选择要打开的文件；`SaveFileDialog` 用于让用户选择保存位置。
- `Stream`（流）是“按字节连续读写数据”的抽象。文件、内存、网络都可以用流方式处理。
- `using` 语句可以确保流用完后**自动释放**；如果不释放，文件可能被占用，网络连接可能不能及时关闭。
- 序列化把对象状态转换为可存储或传输的形式；反序列化把这些数据还原为对象。

### 关键 API / 类 / 方法

- `File.Exists` / `File.Delete`：判断和删除文件。
- `File.WriteAllLines(path, lines, encoding)`：写入字符串数组。
- `File.ReadAllLines(path, encoding)`：读取为字符串数组。
- `File.AppendAllText(path, text, encoding)`：追加字符串。
- `FileStream(path, FileMode, FileAccess)`：以指定模式和权限打开文件流。
- `FileMode.CreateNew` / `Create` / `Open` / `OpenOrCreate` / `Truncate` / `Append`：指定打开文件的行为。[[FileMode讲解]]
- `FileStream.Read` / `FileStream.Write`：按字节数组读写。
- `MemoryStream`：以字节数组和内存作为后备设备，适合缓存、加密、序列化中转。
- `NetworkStream`：用于面向连接的套接字，常由 `TcpClient.GetStream()` 获取。
- `DataContract` / `DataMember` / `DataContractSerializer`：声明和执行对象序列化。

### 代码模式或流程

#### 3.1 一次性写入和读取文本文件

```csharp
string path = @"c:\temp\MyTest.txt";
// 如果文件存在就删除文件，方便之后一次性写入
if (File.Exists(path))
{
    File.Delete(path);
}

string[] lines = { "单位", "姓名", "成绩" };
File.WriteAllLines(path, lines, Encoding.Default);

string[] readText = File.ReadAllLines(path, Encoding.Default);  // 每一行作为一个字符串
Console.WriteLine(string.Join(Environment.NewLine, readText));
```
[[Join方法]]
- 这是课件中创建、写入、读取文本文件的基本模式。
- 实际项目建议优先使用明确编码，例如 `Encoding.UTF8`，减少不同机器默认编码不同导致的乱码。

#### 3.2 FileStream 分块读取

```csharp
public void ReadFromFile(string path)
{
    using (FileStream fs = File.OpenRead(path))  // 打开文件：只读，using 实现读完自动关闭文件
    {
        byte[] bytes = new byte[1024];
        int count = fs.Read(bytes, 0, bytes.Length);
        while (count > 0)
        {
            // 处理 bytes[0..count]
            count = fs.Read(bytes, 0, bytes.Length);
        }
    }
}
```
- [[Read函数]]
- `Read` 返回实际读取的字节数，不一定等于缓冲区长度；循环结束条件通常是返回 0。
- 分块处理适合大文件，避免一次性把整个文件读入内存。

#### 3.3 NetworkStream 读写

```csharp
TcpClient client = new TcpClient();
client.Connect("www.abcd.com", 51888);  // 连接服务器，指定 ip/域名 + 端口；
NetworkStream stream = client.GetStream();  // 获取数据通道，GetStream方法返回网络数据流连接

if (stream.CanWrite)  // 数据通道能否写入
{
    byte[] data = Encoding.UTF8.GetBytes("Hello");  // 字符串转为字节数组，因为TCP为字节流
    stream.Write(data, 0, data.Length);  // 从 data[0] 开始，写入 data.Length 个字节，后发送到服务器
}

if (stream.CanRead)  // 数据通道能否读取
{
    byte[] buffer = new byte[1024];
    int count;
    while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)   // count既可以用于判断是否读完，也可以记录之后需要处理的字节长度
	{
	    // 处理 buffer[0..count]
	}
}
```

- `NetworkStream.Write` 把数据从进程缓冲区送到 TCP 发送缓冲区，之后由 TCP/IP 协议栈发送到网络。
- `Read` 从 TCP 接收缓冲区读到应用程序缓冲区；实际读到多少取决于当前网络和缓冲状态。

#### 3.4 DataContractSerializer 序列化
- GetBytes 是把字符串转化问字节流
- 序列化时把对象转化为字节流
```csharp
[DataContract]   // 标记一个类可以进行数据序列化/传输的特性
public class MyData
{
    [DataMember]  // 标记需要参与序列化的字段
    public string Name { get; set; } = "";  // 自动属性写法（调用还是隐式调用，`对象.属性名` 进行赋值和读取即可），默认值为空
}

public static byte[] Serialize<T>(T obj)  // 泛型写法，支持任意类型 T，包括自定义类型 Object
{
    using MemoryStream ms = new();  // 创建一个内存流
    DataContractSerializer ser = new(typeof(T));  // 创建序列化容器，指定 T 类型
    ser.WriteObject(ms, obj);  // 把对象 obj 写入字节流 ms
    return ms.ToArray();  // 从内存中读取出来，返回字节数组
}
```

- `MemoryStream` ： 内存流，在内存中进行字节读写操作，不依赖文件或者网络
- 被 `[DataMember]` 标记的成员才会进入数据协定序列化；未标记的属性或字段不会被保存。
- 多机协同绘图项目用序列化传输图形图像列表，也用它在服务端或客户端退出时备份绘制内容。

### 初学者易错点

- 忘记指定编码，导致中文读写乱码。
- 用 `FileMode.Create` 打开已存在文件会覆盖原文件；如果不想覆盖，应选择合适的 `FileMode`。
- `Read` 返回的是实际读到的字节数，不能默认整个缓冲区都是有效数据。
- 流没有关闭会占用文件或连接；优先用 `using` 管理生命周期。
- `NetworkStream` 只适用于面向连接的套接字，典型搭配是 TCP，不是 UDP。

### 典型应用场景

- 文本文件读写：保存配置、日志、学生成绩等简单文本数据。
- FileStream：复制文件、处理图片/二进制资源、分块传输文件。
- MemoryStream：先把对象序列化为内存字节，再发送到网络或保存到文件。
- NetworkStream：TCP 客户端和服务端之间发送字节或文本。

### 复习检查清单

- 我能解释编码和解码为什么影响中文文件吗？
- 我能区分 `WriteAllText`、`WriteAllLines`、`AppendAllText` 吗？
- 我知道 `FileMode.Create` 和 `FileMode.Open` 的差别吗？
- 我能说明序列化在网络传输和持久化中的作用吗？

---

## 4. 数据库、DataGridView、LINQ 与 EF Core

### 学习目标

- 理解 SQL Server LocalDB 的定位，以及命名实例 `MSSQLLocalDB` 和连接字符串的作用。
- 掌握 DataGridView 的用途：展示和编辑表格数据，尤其是数据库查询结果。
- 理解 EF Core（Entity Framework Core）的价值、模型类、数据上下文类和开发模式。
- 掌握 LINQ 查询三步：获取数据源、创建查询、执行查询。
- 能用 EF Core 和 LINQ 完成查询、插入、更新、删除，以及必要时执行原始 SQL。

### 核心概念

- SQL Server LocalDB 是轻量级 SQL Server 数据库，适合本地开发和课程示例。
- LocalDB 命名实例默认常写作 `(localdb)\MSSQLLocalDB`，连接字符串中要用它指明服务器部分。
- 示例数据库 `MyDb1.mdf` 包含学生基本信息表（JBXX）、课程编码表（KCBM）、课程成绩汇总表（KCCJ）等结构。
- DataGridView 是 .NET 中显示和编辑表格数据的控件，常用于展示数据库查询结果。
- EF Core 把数据库表映射成 C# 模型类，把数据库连接和表集合组织到数据上下文类中，减少手写 SQL 和数据映射工作。
- Code First 是先定义 C# 类再生成数据库结构；Database First 是先有数据库，再用反向工程工具生成模型类和上下文类。课程示例主要走 Database First。
- LINQ（Language Integrated Query，语言集成查询）让你用 C# 语法查询对象、XML、数据库和泛型集合，并获得强类型检查和统一语法。

### 关键 API / 类 / 方法

- `DbContext`：数据库上下文类，一个数据库通常对应一个上下文类，包含连接信息和实体集合。
- `DbSet<TEntity>`：与表类似，是实体类型实例的容器。
- `SaveChanges`：提交 EF Core 对数据库的增删改。
- `ToList` / `ToArray`：强制立即执行 LINQ 查询，把结果转换成集合。
- `from` / `where` / `orderby` / `group` / `select` / `let`：常用 LINQ 查询子句。
- `FromSql` / `FromSqlRaw`：执行原始 SQL 查询并返回结果。
- `ExecuteSql` / `ExecuteSqlRaw`：执行原始 SQL 命令并返回影响行数。

### 代码模式或流程

#### 4.1 创建数据库与表结构的学习顺序

1. 在项目中创建 `MyDb1.mdf` 数据库文件。
2. 查看数据库连接字符串，理解服务器、数据库文件、认证方式等信息。
3. 创建表结构，例如 JBXX、KCBM、KCCJ，并确认主键、外键、字段类型和是否允许 NULL。
4. 使用 DataGridView 展示查询结果。

#### 4.2 从数据库生成模型类和上下文类

- 先重新生成解决方案，确保无错误，否则 EF Core Power Tools 后续步骤可能失败且不一定给出明确原因。
- 打开数据库文件，让工具识别要连接的数据库。
- 右击项目，添加 `EF Core Database First Wizard`，选择数据库对象，生成模型类和数据上下文类。

#### 4.3 初始化数据库数据

```csharp
using var c = new MyDb1Context();  // 映射已有数据库 MyDb1Context，并建立连接，using 实现自动释放资源

// KCBM 是实体类，每个实体对象对应数据库表的一条记录
List<KCBM> kcbm = new()
{
    new KCBM { Id = "001", KCMingCheng = "C++程序设计", KCLeiBie = "专业基础课" },  //对象初始化器写法，前面的 new() 先创建空对象，此处进行复制
    new KCBM { Id = "002", KCMingCheng = "C#程序设计", KCLeiBie = "专业选修课" },
};

c.KCBMs.AddRange(kcbm);  // 把列表（list）中的数据加入 EF Core 追踪状态，标记为待插入状态，还能没有真正写入数据库
int n = c.SaveChanges();  //真正实行 sql 语句，写入数据，返回影响的行数，此处为 2
```

- 模型类对象类似表中的行；`DbSet` 类似表；`SaveChanges` 才是真正提交到数据库的动作。

#### 4.4 LINQ 查询三步

```csharp
using var c = new MyDb1Context();  //先创建数据库上下文，即建立连接

// LINQ 查询
// var 此处指代：IQueryable<JBXX>
var query = from t in c.JBXXes  // JBXXes 是一个表， t 是指代每一行的临时变脸
            where t.XingMing.StartsWith("李") && t.XingBie == "男"  // where 语句
            select t;  // 返回整行的数据，即一个实体类对象

// DataGridView 是表格显示和编辑数据的控件，此处将查询结果显示到表格控件中
dataGridView_where.DataSource = query.ToList();  // 强制执行LINQ查询（EFCore自动转换为SQL），把查询结果转化为 List 集合
```

转换为 `sql` 语句：
~~~sql
SELECT *
FROM JBXXes
WHERE XingMing LIKE '李%'
  AND XingBie = '男';
~~~

- 获取数据源：`c.JBXXes`。
- 创建查询：`from ... where ... select ...`。创建查询变量本身不一定立即执行。
- 执行查询：`ToList()` 或 `foreach` 触发执行。

#### 4.5 多表查询与排序

```csharp
var q = from t1 in c.JBXXes
        from t2 in c.KCCJs
        where t1.XueHao == t2.XueHao && t2.ChengJi > 85
        orderby t2.XueHao ascending, t2.ChengJi descending
        select new
        {
            学号 = t2.XueHao,
            姓名 = t1.XingMing,
            成绩 = t2.ChengJi
        };

dataGridView_orderby.DataSource = q.ToList();
```

- 这个例子体现了多表匹配、条件筛选、排序和匿名对象投影。

### 初学者易错点

- 只创建了实体对象但忘记 `SaveChanges`，数据库不会更新。
- 创建 LINQ 查询变量后以为已经执行；实际上很多查询是延迟执行。
- EF Core Power Tools 前没有重新生成解决方案，后续生成模型失败。
- DataGridView 只是展示/编辑控件，不等于数据库本身；数据源、绑定对象和数据库提交是不同层次。
- 日期和图片这类数据在 DataGridView 中需要额外处理，例如日期可用 `yyyy-MM-dd` 显示，编辑时用 DateTimePicker；图片导入时要先定位到绑定实体对象。

### 典型应用场景

- 学生信息、课程、成绩管理：用 LocalDB 存储，DataGridView 展示，LINQ 查询筛选。
- 数据维护界面：插入、编辑、更新、删除记录，结合 DataGridView 和 EF Core。
- 需要复杂 SQL 或性能优化时：用 EF Core 原始 SQL 方法补充 LINQ。

### 复习检查清单

- 我能解释 LocalDB 和 SQL Server 的关系吗？
- 我知道连接字符串里 `(localdb)\MSSQLLocalDB` 的意义吗？
- 我能区分模型类、数据上下文类、DbSet 吗？
- 我能写出 LINQ 查询的三步吗？

---

## 5. IP、DNS、进程与线程

### 学习目标

- 理解 IP 地址、子网掩码、端口号和 IPEndPoint 的意义。
- 掌握 `IPAddress`、`Dns`、`IPHostEntry` 的基本使用。
- 理解进程（Process）和线程（Thread）的区别。
- 掌握 Thread、ThreadPool、后台线程、Sleep、WinForms 跨线程 UI 访问、lock 同步的基础。

### 核心概念

- IP 地址用于标识联网主机的位置；IPv4 使用点分十进制，4 个字节；IPv6 使用 128 位，常用 8 段十六进制表示。
- IPv4 地址由网络号和主机号组成；子网掩码用 1 标识网络位，用 0 标识主机位。
- 端口是进程通信标识，范围 0 到 65535；知名端口 0 到 1023，应用程序一般使用大于 1023 的端口。
- `IPEndPoint` 把 **IP 地址和端口组合起来**，表示应用程序连接主机服务所需的端点信息。
- DNS 域名解析把域名转换为 IP 地址；解析结果受网络和域名服务器状态影响，可能成功也可能失败。
- 进程是“正在运行的程序”，具有程序、资源和内存边界；线程是进程中的独立执行流。
- 前台线程会影响进程终止；后台线程不影响进程终止，当所有前台线程结束后后台线程会被立即停止。
- 并发线程访问共享资源时要同步，避免争用和死锁；`lock` 可保护临界区。

### 关键 API / 类 / 方法

- `IPAddress.Parse`：把字符串转换为 IPAddress（ip地址），建议配合异常处理。
- `IPAddress.AddressFamily`：判断 IPv4 或 IPv6。
- `IPAddress.Any` / `Broadcast` / `Loopback`：特殊地址，分别常用于监听所有 IPv4 接口、广播、回环地址。
- `Dns.GetHostEntry`：把主机名或 IP 地址解析为 `IPHostEntry`。
- `Dns.GetHostName`：获取本机主机名。
- `Process.Start` / `Process.GetProcesses` / `GetProcessById` / `GetProcessByName` / `Kill` / `CloseMainWindow`：进程启动、查询、停止。
- `Thread` / `Thread.Start` / `Thread.Sleep` / `IsBackground`：创建、启动、休眠和设置后台线程。
- `ThreadPool.QueueUserWorkItem`：把工作项加入托管线程池。
- `Control.Invoke` / `InvokeAsync`：在 WinForms 中从后台线程安全更新 UI。
- `lock`：保护临界区。

### 代码模式或流程

#### 5.1 DNS 域名解析
- entry：条目
- `IPHostEntry` ：用于封装 DNS 解析结果的类，保存主机名对应的多个 IP 地址和主机名
- `IPAddress` : 存放 IP 地址的类
```csharp
IPHostEntry entry = Dns.GetHostEntry("www.henu.edu.cn");   // DNS解析，将域名解析为网络信息对象，
IPAddress[] addresses = entry.AddressList;  // 获得该网站对应的所有 IP 地址
string hostName = entry.HostName;  // 获得主机名
```

- `AddressList` 包含与主机关联的 IP 地址列表，可能同时包含 IPv4 和 IPv6。
- DNS 解析依赖网络和 DNS 服务器，实验时要准备异常处理和失败提示。

#### 5.2 启动进程

```csharp
Process p = new Process();  // 创建一个进程对象
// 设置启动信息：第一行传入启动目标，第二行给程序传入参数，例如记事本打开某个文件或者浏览器打开某个网页
p.StartInfo.FileName = "notepad.exe";
p.StartInfo.Arguments = "";

p.Start();  // 启动线程
Process.Start("calc.exe");  // 也可以直接启动进程，此处为计算器
```

- 如果只传文件名，系统通常先在当前目录查找，再到环境变量 Path 指定路径中查找；找不到就需要补全应用程序路径并加异常处理。

#### 5.3 创建并启动线程

- 创建一个后台线程来执行 `Method1` 中的任务
```csharp
Thread t1 = new Thread(Method1);  // 创建线程，参数表示该线程执行的方法
t1.IsBackground = true;  // 设置为后台线程，进程结束时后台线程会强制关闭，而前台线程会阻止程序关闭
t1.Start();

void Method1()
{
    // 后台任务
}
```

- `Thread` 可通过委托创建线程；不带参数方法可用 `ThreadStart`，带参数方法可用 `ParameterizedThreadStart`（在`start()`中传入参数）。
- `Start` 后当前线程会继续执行后续代码，不会等待子线程结束。

#### 5.4 WinForms 后台线程更新 UI

- `() => {}` 是 lambda，作为此处 Invoke() 函数传入的一个事件参数
```csharp
textBox1.Invoke(() =>
{
    textBox1.Text += s;
});
```

- .NET 默认不允许在一个线程中直接访问另一个线程创建的控件，因为多个线程同时访问控件可能导致不确定状态甚至死锁。
- 即只能在创建控件中的线程（UI线程）中操控控件

#### 5.5 lock 保护临界区

- `readonly` 字段表示只读
- `decimal` 是高精度小数类型，后面要加上 m：`decimal p = 3.14m`，`double` 为二进制浮点数，`decimal` 是十进制精准计算
```csharp
private readonly object lockedObj = new object();  // 创建一个 锁标记对象

void Withdraw(decimal amount)
{
    lock (lockedObj)  // lock 关键字实现线程同步（加锁）：同一时刻只有一个线程执行
    {
        // 检查余额、扣款、写回余额
    }
}
```

- `lock` 代码块称为临界区，代码不宜过多；提款机示例用于说明多线程共享账户余额时必须同步。

### 初学者易错点

- 端口是**进程通信标识**，不是主机标识；确定一个网络服务通常需要 IP 地址 + 端口号。
- DNS 解析失败不一定是代码错，也可能是网络或 DNS 服务器问题。
- `Kill` 会非正常终止进程，可能导致未保存数据丢失；有界面的程序优先尝试 `CloseMainWindow`。
- `Thread.Sleep` 暂停的是当前线程，不能从一个线程随意暂停另一个线程。
- 线程池适合短生命周期任务和高并发异步操作，但加入线程池的任务不一定立即执行，线程池线程默认是后台线程。
- 在 WinForms 后台线程里直接改 TextBox、ListBox 很容易出错，应该通过 `Invoke` 或 `InvokeAsync` 回到 UI 线程。

### 典型应用场景

- IP/DNS 工具：输入域名，解析并显示 IP 地址列表。
- 进程管理器：列出本机进程、显示进程信息、启动或关闭指定进程。
- 多线程 IP 扫描：把多个地址的扫描任务分给多个线程或线程池，提高整体速度。
- WinForms 后台任务：后台线程执行耗时任务，UI 线程只负责界面显示和交互。

### 复习检查清单

- 我能解释 IP、端口、IPEndPoint 三者关系吗？
- 我能写出一个 DNS 解析示例并处理失败吗？
- 我能说出进程与线程的区别吗？
- 我知道前台线程和后台线程的区别吗？

---

## 6. TCP 应用编程

### 学习目标

- 理解 TCP 在网络分层中的位置、C/S 通信模式和适用场景。
- 掌握 `TcpClient`、`TcpListener`、`NetworkStream` 的职责和基本流程。
- 理解 TCP 无消息边界问题，并知道三类常见解决办法。
- 能区分同步 TCP 和异步 TCP 的执行方式。
- 能读懂聊天程序、棋子消消乐、多机协同绘图等 C/S 项目的命令协议和类设计。

### 核心概念

- TCP（Transmission Control Protocol，传输控制协议）位于传输层，常用于需要可靠连接的 C/S 网络应用，例如即时聊天、大型网络游戏、股票交易、行业应用。
- 服务端**不断监听**客户端连接请求，连接建立后进行通信；客户端主动连接服务端，然后与服务端通信。
- `TcpClient` 是客户端常用类，封装底层 Socket，提供连接远程主机、获取 NetworkStream、关闭连接等功能。
- `TcpListener` 是服务端常用类，用于监听本地 IP 和端口，接受客户端连接。
- TCP 是**字节流**协议，不能保证一次发送的消息被一次接收；这就是“无消息边界问题”。
- 同步 TCP：执行到发送、接收或监听语句时，在**完成前阻塞后续代码**；异步 TCP：调用后程序继续往下执行，**结果通过任务或回调得到（返回任务类型）**。
- TAP（Task-based Asynchronous Pattern，基于任务的异步模式）是现代 .NET 推荐的异步方式，常与 `async` / `await` 配合。

### 关键 API / 类 / 方法

- `new TcpClient()` + `Connect(host, port)`：创建客户端并连接远程主机。
- `TcpClient.GetStream()`：获取 `NetworkStream` 用于发送和接收数据。
- `TcpClient.Close()`：释放并关闭 TCP 连接。
- `new TcpListener(IPAddress, port)` / `new TcpListener(IPEndPoint)`：创建监听器。
- `TcpListener.Start(backlog)`：启动监听，`backlog` 表示连接请求队列最大长度。
- `TcpListener.AcceptTcpClient()`：同步阻塞接受连接，返回 `TcpClient`。
- `TcpListener.Stop()`：停止监听，不关闭已经接收的连接。
- `AcceptTcpClientAsync()` / `ConnectAsync()`：TAP 风格异步接受连接和异步连接远程主机。

### 代码模式或流程

#### 6.1 TCP 服务端同步基本流程

```csharp
TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 51888);  // 创建一个服务端监听器，此处只允许本机地址（127.0.0.1）访问
listener.Start();  //开始监听

while (true)
{
    TcpClient newClient = listener.AcceptTcpClient();  // 阻塞等待客户端的连接，同步线程
    NetworkStream stream = newClient.GetStream();  // 获得数据连接通道
    // 与连接的客户端通信
}
```

- 服务端流程：创建 `TcpListener`，启动监听，循环 `AcceptTcpClient`，获得 `TcpClient`，与客户端通信，最后停止监听。
- 因为 `AcceptTcpClient` 是同步阻塞的，真实服务端通常要为每个客户端开线程、任务或异步循环。

#### 6.2 TCP 客户端同步基本流程

```csharp
using TcpClient client = new TcpClient();
client.Connect("abcd.com", 51666);
NetworkStream stream = client.GetStream();

// stream.Read(...); stream.Write(...);

// client.Close();
```

- 客户端流程：创建 `TcpClient`，连接服务端，通过网络流通信，通信结束后断开连接。

#### 6.3 解决 TCP 无消息边界

| 方法              | 思路                           | 适用场景             |
| ----------------- | ------------------------------ | -------------------- |
| 固定长度消息      | 每条消息长度固定，不足补齐     | 消息格式非常稳定     |
| 消息长度 + 消息体 | 先发送长度，再发送对应长度内容 | 通用场景，最可靠     |
| 特殊标记分隔      | 用换行或特殊字符分隔消息       | 消息内容不包含该标记 |

- 课件给出三种解决办法：固定长度、长度与消息一起发送、特殊标记分隔。
- 初学者容易以为 `Read` 一次就能读到对方 `Write` 的完整字符串，这是 TCP 编程最常见误区。

#### 6.4 聊天程序命令协议

- 登录：`Login,用户名`，服务端收到后通知当前在线用户。
- 群聊：`Talk,对话信息`，服务端收到后转发给在线用户。
- 退出：`Logout,用户名`，服务端清理用户并通知其他人。
- 服务端通常要封装每个客户端的信息：客户端 `TcpClient`、客户端名称、与该客户端通信的网络流对象。

#### 6.5 异步 TCP 基本写法
- 异步 TCP 服务端，实现持续监听 + 并发处理客户端
- 异步等待就是让代码像同步一样写，但不会卡线程
```csharp
private async Task StartServerAsync(IPAddress address, int port)
{
    TcpListener listener = new TcpListener(address, port);
    listener.Start();

    while (true)
    {
        TcpClient client = await listener.AcceptTcpClientAsync();  // 异步等待客户端连接，不阻塞其他线程，例如 UI
        // 把任务丢到线程池中执行，自动分配线程
        _ = Task.Run(() => HandleClientAsync(client));  // 异步处理每一个客户端，`_=` ：忽略返回Task（代表任务），即不等待
        // 如果不忽略，可写为：Task.Run(() => HandleClientAsync(client)); 会出现编译警告（未等待Task），如果 await 又会阻塞新的客户端连接
    }
}

private async Task ConnectAsync(IPAddress address, int port)
{
    TcpClient client = new TcpClient();
    await client.ConnectAsync(address, port);  // 每个客户端异步建立连接
}
```

- `AcceptTcpClientAsync` 会返回 `Task<TcpClient>`，用 `await` 得到异步执行结果。
- `ConnectAsync` 异步发起连接请求，`await` 等待完成时不会影响用户对 UI 界面的操作。
- 异步事件处理程序可写成 `async void ButtonOK_Click(...)`，普通异步方法应优先返回 `Task` 或 `Task<T>`。

#### 6.6 棋子消消乐项目的命令设计

- 客户端发送给服务端：`Login`、`Logout`、`SitDown`、`GetUp`、`Level`、`Start`、`UnsetDot`、`Talk` 等。
- 服务端发送给客户端：`Sorry`、`Tables`、`SitDown`、`GetUp`、`Lost`、`Message`、`Level`、`Start`、`SetDot`、`UnsetDot`、`Win`、`Talk` 等。
- 重点不是死记命令，而是理解网络项目要先设计协议：命令名、参数格式、谁发送、谁接收、收到后做什么。

#### 6.7 多机协同绘图项目的架构

- 客户端负责接收服务端信息和具体绘图操作；服务端提供通信服务，实现多机协同绘图。
- 客户端绘制或移动对象时先向服务端发送命令，服务端再广播给其他客户端，所有客户端按同一命令更新界面，从而保持一致。
- 绘图对象抽象基类保存公共属性和方法，矩形、椭圆、曲线、箭头曲线、文本、图像等从基类派生，通过多态重写 `Draw` 方法。
- 图形对象列表用 `GraphicsList` 管理，负责查找、删除、选择和序列化；客户端还用 `MyTcpClient` 封装收发信息处理，服务端用 `E0706TcpServer` 和 `E0706User` 管理连接。
- 对象 ID 必须由服务端统一分配，因为多台机器同时绘制时，每个对象都要有全局唯一标识。
- 序列化用于新客户端登录时同步已有图形列表，也用于服务端退出时保存半成品，下次启动后继续绘制。

### 初学者易错点

- 把 TCP 当成“发一次字符串，对方收一次字符串”。TCP 是流，没有天然消息边界，必须自己设计分隔方式或长度协议。
- 同步 TCP 在 `AcceptTcpClient`、`Read` 等位置会阻塞；如果写在 UI 线程，界面可能卡死。
- `TcpListener.Stop()` 停止监听，但不会自动关闭已经接收的连接，连接对象仍要单独管理。
- 聊天、游戏、协同绘图这类项目必须先设计命令协议，否则后面解析字符串会混乱。
- `async void` 只适合事件处理程序，普通异步方法应返回 `Task` 或 `Task<T>`，便于等待和异常处理。

### 典型应用场景

- 群聊程序：客户端登录、发言、退出；服务端维护在线用户并转发消息。
- 棋子消消乐：服务端管理游戏室、座位、难度、棋子生成、胜负消息；客户端解析命令更新界面。
- TCP 即时通信实验：客户端 WinForms，服务端控制台，设计 `Login`、`TalkToOne`、`Talk`、`List` 等命令，支持单聊和群聊。
- 多机协同绘图：客户端绘制对象，服务端广播命令并保存对象列表，实现多台机器同步绘图。

### 复习检查清单

- 我能画出 TCP 服务端和客户端的基本流程吗？
- 我能解释 TCP 无消息边界问题吗？
- 我知道 `TcpClient`、`TcpListener`、`NetworkStream` 分别做什么吗？
- 我能区分同步 TCP 和异步 TCP 吗？
- 我能设计一个简单命令协议，例如 `Login,user`、`Talk,message`、`Logout,user` 吗？

---

## 7. UDP 应用编程

### 学习目标

- 理解 UDP 与 TCP 的差异：速度、连接、可靠性、顺序、消息边界、一对多传输。
- 掌握 `UdpClient` 构造函数、发送、接收、异步发送/接收。
- 理解广播（Broadcast）和组播（Multicast）的区别和使用场景。
- 能读懂 UDP 网络会议程序的功能和消息格式。

### 核心概念

- UDP（User Datagram Protocol，用户数据报协议）构建于 IP 之上，速度通常比 TCP 快，因为它不需要先建立连接，也不等待传输确认。
- UDP 有消息边界，发送一个数据报，接收端按数据报接收，不像 TCP 那样要自己处理消息边界。
- UDP 可以一对多传输，适合广播和组播。
- UDP 不保证可靠传输，也不保证有序传输；如果数据报丢失，UDP 协议本身不会检测或提示。
- `UdpClient` 是 .NET 中封装 UDP 套接字的常用类，因为 UDP 无连接，所以不像 TCP 那样区分 `TcpClient` 和 `TcpListener`。

### 关键 API / 类 / 方法

- `new UdpClient()`：自动分配本地 IPv4 地址和端口，发送前通常要 `Connect` 或在 `Send` 中指定远程端点。
- `new UdpClient(IPEndPoint localEp)`：绑定本地 IP 和端口，适合明确指定本地接收端口。
- `new UdpClient(string hostname, int port)`：创建并指定默认远程主机。
- `Connect`：建立默认远程主机；注意 UDP 的 Connect 不会像 TCP 那样阻塞建立连接。
- `Send` / `SendAsync`：同步或异步发送数据报。
- `Receive` / `ReceiveAsync`：同步或异步接收数据报。
- `JoinMulticastGroup` / `DropMulticastGroup`：加入或退出组播组。
- `EnableBroadcast`：控制是否接收或发送广播包。

### 代码模式或流程

#### 7.1 指定远程端点发送 UDP 数据

```csharp
UdpClient udpClient = new UdpClient();
IPAddress remoteIp = IPAddress.Parse("192.168.0.10");
IPEndPoint remoteEndPoint = new IPEndPoint(remoteIp, 18001);

byte[] sendBytes = Encoding.Unicode.GetBytes("你好!");
udpClient.Send(sendBytes, sendBytes.Length, remoteEndPoint);
```

- `Send(byte[] data, int length, IPEndPoint iep)` 把 UDP 数据报发送到指定远程端点，返回已发送字节数。

#### 7.2 接收 UDP 数据

```csharp
IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
byte[] data = udpClient.Receive(ref remoteEP);
string message = Encoding.Unicode.GetString(data);
```

- `Receive(ref IPEndPoint remoteEP)` 返回接收到的字节数组，并通过 `remoteEP` 得知发送方 IP 和端口。

#### 7.3 广播

- 广播是向子网内多台计算机同时发送消息；本地广播只影响本地子网，全球广播使用 `255.255.255.255`，但路由器通常会过滤全球广播。
- 本地广播地址由网络标识和主机标识决定：主机位全为 1 的地址就是该子网广播地址。例如 `192.168.0.0/24` 的本地广播地址是 `192.168.0.255`。

#### 7.4 组播

```csharp
UdpClient udpClient = new UdpClient(8001);
udpClient.JoinMulticastGroup(IPAddress.Parse("224.100.0.1"));

// ... 接收或发送组播数据 ...

udpClient.DropMulticastGroup(IPAddress.Parse("224.100.0.1"));
```

- 组播是把消息发送给加入指定组播组的计算机集合；IPv4 组播地址范围为 `224.0.0.0` 到 `239.255.255.255`。
- `JoinMulticastGroup` 加入组播组，底层 Socket 会向路由器请求成为组成员；地址不合法或路由器不支持组播时可能抛出 `SocketException`。
- `DropMulticastGroup` 退出组播组，退出后不再接收该组播组数据报。

#### 7.5 UDP 网络会议程序消息格式

- `Login`：用户请求进入会议室，其他接收方提示有新用户进入，并向发送方返回已有人员列表。
- `Logout`：用户请求退出会议室，接收方提示用户离开并从人员列表删除。
- `List,用户列表信息`：接收方把非重复用户信息添加到会议室成员列表。
- `Message,发言信息`：会议发言消息，接收方直接显示发言内容。

### 初学者易错点

- UDP 的 `Connect` 不是 TCP 的连接建立；它只是指定默认远程主机，UDP 仍然是无连接的。
- 如果打算接收多路广播数据报，不要随便调用 `Connect`，否则来自默认地址以外的数据报可能被丢弃。
- UDP 不保证可靠和有序，不能把它直接当成 TCP 的轻量替代品；重要消息要自己设计重传、确认、序号等机制，或者改用 TCP。
- 组播地址必须在合法范围内，且网络设备要支持组播，否则加入组播可能失败。

### 典型应用场景

- 局域网公告、设备发现：可用广播快速向同一子网内主机发送信息。
- 网络会议室：用组播让加入会议的成员互相收到登录、退出、发言和人员列表消息。
- 对实时性要求高、可容忍少量丢包的场景：可考虑 UDP，但必须理解可靠性和顺序风险。

### 复习检查清单

- 我能说出 UDP 相比 TCP 的优点和代价吗？
- 我能写出 `UdpClient.Send` 的常见使用方式吗？
- 我知道 `Receive(ref IPEndPoint)` 为什么要传 `ref` 端点吗？
- 我能解释广播地址如何由子网掩码决定吗？
- 我知道 `JoinMulticastGroup` 和 `DropMulticastGroup` 的作用吗？

---

## 8. 综合项目思维：从控件到协议到系统

### 学习目标

- 能把前面章节串起来：WinForms 做界面，线程避免阻塞，流传输数据，TCP/UDP 完成通信，序列化保存对象，数据库保存结构化信息。
- 能理解一个 C/S 网络应用通常不仅是“会发消息”，还包含界面、协议、并发、数据结构、异常处理和状态同步。

### 综合理解

- 控件层：WinForms 的 Label、TextBox、Button、ListBox、DataGridView、PictureBox 等负责用户交互和数据显示。
- 数据层：文件、流、数据库、EF Core、序列化分别解决不同形态的数据保存和转换。
- 网络层：TCP 适合可靠连接和复杂 C/S 协议；UDP 适合广播、组播和对实时性更敏感的场景。
- 并发层：同步调用容易阻塞，线程、线程池、Task、async/await 用来保持程序响应；WinForms 更新 UI 时要通过 Invoke。
- 协议层：聊天、游戏、会议、协同绘图都要设计命令格式，明确命令、参数、发送方、接收方和动作。
- 系统层：多机协同绘图体现了完整系统设计：对象抽象、工具类、通信类、服务端广播、对象 ID、序列化、持久化和多客户端一致性。

### 一个初学者可执行的练习顺序

1. 写一个控制台程序，输入两个数并输出结果。
2. 写一个 WinForms 登录窗体，成功后打开主窗体。
3. 写一个文本文件读写工具，用 OpenFileDialog 打开文件，用 SaveFileDialog 保存文件。
4. 写一个 LocalDB + DataGridView 查询界面，用 LINQ 显示学生信息。
5. 写一个 DNS 解析工具，输入域名，显示 IP 地址列表，并处理失败。
6. 写一个 TCP 单客户端回显程序，再改成多客户端聊天。
7. 把同步聊天改为 async/await 版本，保证 UI 不阻塞。
8. 写一个 UDP 组播会议室，实现 Login、Logout、List、Message。
9. 读多机协同绘图项目，重点画出类图和命令流，而不是一开始就硬啃全部代码。

### 总复习检查清单

- 我能解释“界面、业务逻辑、数据保存、网络通信、并发控制”各自在哪一层吗？
- 我能根据需求判断该用 TCP 还是 UDP 吗？
- 我能为一个聊天或会议程序设计命令格式吗？
- 我知道哪里可能阻塞，哪里需要线程或异步吗？
- 我能解释序列化为什么能支持“发送对象”和“下次继续绘制”吗？

---

## 附录 A. 13 份课件覆盖情况

- `1-概述-2026.pdf`：C#、.NET、Visual Studio、项目/解决方案、命名空间、Main、调试、网络模型。
- `2-控制台和WinForms应用编程入门-2025-02-21.pdf`：控制台输入输出、格式化、WinForms、常用控件、图像控件。
- `5.1-文件读写.pdf`：编码、File 类、文本文件读写、追加、文件对话框。
- `5.2-数据库与DataGridView控件.pdf`：LocalDB、数据库和表结构、DataGridView。
- `5.3-利用LINQ和EF Core操作数据库.pdf`：EF Core、Database First、LINQ 查询、增删改、原始 SQL、完整数据库示例。
- `6.1-IP地址转换与域名解析-6.2进程线程.pdf`：IP、端口、DNS、Process、Thread、ThreadPool、Invoke、lock。
- `6.4-数据流.pdf`：FileStream、MemoryStream、NetworkStream、StreamReader/Writer、序列化。
- `7.1  TCP应用编程预备知识.pdf`：TCP 基础、TcpClient、TcpListener、无消息边界。
- `7.2  同步TCP应用编程.pdf`：同步 TCP 流程、群聊、棋子消消乐命令协议。
- `7.3  异步TCP应用编程.pdf`：APM/EAP/TAP、async/await、异步 TCP、即时通信实验。
- `7.4  TCP应用开发实例-20250429.pdf`：绘图对象、GDI+、协同绘图方案、序列化和命令约定。
- `7.4-多机协同绘图系统开发实例.pdf`：多机协同绘图系统的完整文字版讲解，覆盖类设计、命令、序列化、客户端/服务端实现。
- `8-UDP应用编程.pdf`：UDP 与 TCP 区别、UdpClient、广播、组播、网络会议程序。

## 附录 B. 抽取质量提示

- 本次逐页抽取共覆盖 13 份 PDF、682 页。
- 有 156 页抽取文本较少，集中在封面、章节页、图片页、代码截图页或流程图页；最终总结优先使用可抽取文本清晰的概念页、API 页、流程页和项目讲解页。
- 详细低文字页清单见同目录下 `coverage_check.md`。
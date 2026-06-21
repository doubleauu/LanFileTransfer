// 封装服务端数据库路径、DbContext 创建和启动时数据库自检。
using LanFileTransfer.Data;
using Microsoft.EntityFrameworkCore;

namespace LanFileTransfer.Server;

internal static class ServerDatabase
{
    // 固定位置为程序运行目录下，拼接文件路径
    private static readonly string DatabasePath = Path.Combine(AppContext.BaseDirectory, "LanFileTransfer.db");

    // 创建数据库并做一次读写自检。
    public static void InitializeDatabase()
    {
        // 先用 EnsureCreated 快速生成表结构（不支持版本控制），后续需要迁移时再改为 Migrations。
        using LanFileTransferDbContext dbContext = CreateDbContext();  // 创建数据库上下文
        dbContext.Database.EnsureCreated();  // 如果数据库不存在就创建数据库，自动生成 Dbset 对应的表
        VerifyDatabaseReadWrite(dbContext);  // 检测数据库是否可用

        Console.WriteLine($"数据库已就绪：{DatabasePath}");
    }

    // 根据固定数据库路径创建 EF Core 上下文。
    public static LanFileTransferDbContext CreateDbContext()
    {

        // 创建数据库配置构建器
        DbContextOptions<LanFileTransferDbContext> options = new DbContextOptionsBuilder<LanFileTransferDbContext>()
            .UseSqlite($"Data Source={DatabasePath}")
            .Options;

        return new LanFileTransferDbContext(options);
    }

    // 使用事务写入并查询临时用户，验证数据库可用后回滚。
    private static void VerifyDatabaseReadWrite(LanFileTransferDbContext dbContext)
    {
        // 开启数据库事务，方便之后回滚操作，using 实现用完自动回收资源
        using var transaction = dbContext.Database.BeginTransaction();
        string testUsername = $"__db_test_{Guid.NewGuid():N}";

        dbContext.Users.Add(new User
        {
            Username = testUsername,
            PasswordHash = "test"
        });
        dbContext.SaveChanges();  // 强制执行sql语句

        // 查询刚写入的临时用户，验证 EF Core 新增和查询都能正常工作。
        bool canReadBack = dbContext.Users.Any(user => user.Username == testUsername);
        if (!canReadBack)
        {
            throw new InvalidOperationException("数据库读写验证失败。");
        }

        // 回退该事务的所有操作
        transaction.Rollback();
    }
}

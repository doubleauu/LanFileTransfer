// 封装服务端数据库路径、DbContext 创建和启动时数据库自检。
using LanFileTransfer.Data;
using Microsoft.EntityFrameworkCore;

namespace LanFileTransfer.Server;

internal static class ServerDatabase
{
    private static readonly string DatabasePath = Path.Combine(AppContext.BaseDirectory, "LanFileTransfer.db");

    // 创建数据库并做一次读写自检。
    public static void InitializeDatabase()
    {
        // 作业项目早期先用 EnsureCreated 快速生成表结构，后续需要迁移时再改为 Migrations。
        using LanFileTransferDbContext dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
        VerifyDatabaseReadWrite(dbContext);

        Console.WriteLine($"数据库已就绪：{DatabasePath}");
    }

    // 根据固定数据库路径创建 EF Core 上下文。
    public static LanFileTransferDbContext CreateDbContext()
    {
        DbContextOptions<LanFileTransferDbContext> options = new DbContextOptionsBuilder<LanFileTransferDbContext>()
            .UseSqlite($"Data Source={DatabasePath}")
            .Options;

        return new LanFileTransferDbContext(options);
    }

    // 使用事务写入并查询临时用户，验证数据库可用后回滚。
    private static void VerifyDatabaseReadWrite(LanFileTransferDbContext dbContext)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        string testUsername = $"__db_test_{Guid.NewGuid():N}";

        dbContext.Users.Add(new User
        {
            Username = testUsername,
            PasswordHash = "test"
        });
        dbContext.SaveChanges();

        // 查询刚写入的临时用户，验证 EF Core 新增和查询都能正常工作。
        bool canReadBack = dbContext.Users.Any(user => user.Username == testUsername);
        if (!canReadBack)
        {
            throw new InvalidOperationException("数据库读写验证失败。");
        }

        transaction.Rollback();
    }
}

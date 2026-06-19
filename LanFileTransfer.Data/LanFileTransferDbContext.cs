// 保存 EF Core 数据库上下文和本项目需要的三个数据库实体。
using LanFileTransfer.Common;
using Microsoft.EntityFrameworkCore;

namespace LanFileTransfer.Data;

public class LanFileTransferDbContext : DbContext
{
    // 构造函数，初始化语句写法，接收外部数据库配置并初始化 EF Core 数据上下文。
    public LanFileTransferDbContext(DbContextOptions<LanFileTransferDbContext> options) : base(options)
    {
    }

    // 定义三个数据库访问入口：
    public DbSet<User> Users => Set<User>();

    public DbSet<FileRecord> FileRecords => Set<FileRecord>();

    public DbSet<TransferRecord> TransferRecords => Set<TransferRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 用户名必须唯一，避免注册重复账号。
        modelBuilder.Entity<User>()
            .HasIndex(user => user.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(user => user.Username)
            .HasMaxLength(50);

        modelBuilder.Entity<User>()
            .Property(user => user.PasswordHash)
            .HasMaxLength(200);

        modelBuilder.Entity<FileRecord>()
            .Property(file => file.ResourceType)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<FileRecord>()
            .HasOne(file => file.Uploader)
            .WithMany(user => user.UploadedFiles)
            .HasForeignKey(file => file.UploaderId);

        modelBuilder.Entity<TransferRecord>()
            .Property(record => record.TransferType)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<TransferRecord>()
            .Property(record => record.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        modelBuilder.Entity<TransferRecord>()
            .HasOne(record => record.User)
            .WithMany(user => user.TransferRecords)
            .HasForeignKey(record => record.UserId);

        modelBuilder.Entity<TransferRecord>()
            .HasOne(record => record.File)
            .WithMany(file => file.TransferRecords)
            .HasForeignKey(record => record.FileId);
    }
}

// 用户表，保存登录账号和密码哈希。
// 自动属性写法：
public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<FileRecord> UploadedFiles { get; set; } = new();

    public List<TransferRecord> TransferRecords { get; set; } = new();
}

// 文件信息表，数据库只保存元数据，真实文件保存在服务端磁盘。
public class FileRecord
{
    public int Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;  // 初始化为空字符串

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string? FileHash { get; set; }

    public ResourceType ResourceType { get; set; } = ResourceType.File;

    public int UploaderId { get; set; }

    public User? Uploader { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.Now;

    public List<TransferRecord> TransferRecords { get; set; } = new();
}

// 传输记录表，保存上传和下载的历史信息。
public class TransferRecord
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public int FileId { get; set; }

    public FileRecord? File { get; set; }

    public TransferType TransferType { get; set; }

    public TransferStatus Status { get; set; }

    public long BytesTransferred { get; set; }

    public string ClientIp { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.Now;

    public DateTime FinishedAt { get; set; } = DateTime.Now;
}

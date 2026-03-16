namespace DiskSlim.Models;

/// <summary>
/// 迁移任务状态枚举
/// </summary>
public enum MigrationStatus
{
    /// <summary>等待执行</summary>
    Pending,
    /// <summary>正在迁移</summary>
    InProgress,
    /// <summary>迁移完成</summary>
    Completed,
    /// <summary>迁移失败</summary>
    Failed,
    /// <summary>已回退/撤销</summary>
    RolledBack
}

/// <summary>
/// 迁移任务模型，记录一次文件夹迁移操作的完整信息
/// </summary>
public class MigrationTask
{
    /// <summary>任务唯一标识</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>迁移任务名称（如"迁移文档文件夹"）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>源路径（C盘中的原始路径）</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>目标路径（目标盘上的新路径）</summary>
    public string DestinationPath { get; set; } = string.Empty;

    /// <summary>是否创建了符号链接（Junction）</summary>
    public bool HasSymlink { get; set; }

    /// <summary>迁移的文件总大小（字节）</summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>已迁移大小（字节），用于进度显示</summary>
    public long TransferredBytes { get; set; }

    /// <summary>迁移进度（0.0 ~ 1.0）</summary>
    public double Progress => TotalSizeBytes > 0 ? (double)TransferredBytes / TotalSizeBytes : 0;

    /// <summary>任务状态</summary>
    public MigrationStatus Status { get; set; } = MigrationStatus.Pending;

    /// <summary>错误信息（失败时填充）</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>任务创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>任务完成时间</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>是否可以回退（迁移完成且符号链接正常则可回退）</summary>
    public bool CanRollback => Status == MigrationStatus.Completed && HasSymlink;
}

namespace DiskSlim.Models;

/// <summary>
/// 磁盘扫描快照，记录某一时刻的C盘使用状态
/// </summary>
public class DiskSnapshot
{
    /// <summary>快照唯一 ID（自增主键）</summary>
    public int Id { get; set; }

    /// <summary>快照保存时间</summary>
    public DateTime SnapshotTime { get; set; }

    /// <summary>快照备注名称（用户可自定义）</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>磁盘总大小（字节）</summary>
    public long TotalBytes { get; set; }

    /// <summary>已用空间（字节）</summary>
    public long UsedBytes { get; set; }

    /// <summary>可用空间（字节）</summary>
    public long FreeBytes { get; set; }

    /// <summary>各文件夹大小明细列表</summary>
    public List<SnapshotFolderItem> FolderItems { get; set; } = new();

    // --- 计算属性 ---

    /// <summary>已用空间占比（0.0 ~ 1.0）</summary>
    public double UsagePercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes : 0;

    /// <summary>快照时间格式化文字</summary>
    public string SnapshotTimeText => SnapshotTime.ToString("yyyy-MM-dd HH:mm");

    /// <summary>已用空间格式化文字</summary>
    public string UsedBytesText => Helpers.FileSizeHelper.Format(UsedBytes);

    /// <summary>可用空间格式化文字</summary>
    public string FreeBytesText => Helpers.FileSizeHelper.Format(FreeBytes);

    /// <summary>总大小格式化文字</summary>
    public string TotalBytesText => Helpers.FileSizeHelper.Format(TotalBytes);

    /// <summary>使用率格式化文字</summary>
    public string UsagePercentText => $"{UsagePercent * 100:F1}%";

    /// <summary>显示名称：优先用备注，没有则用时间</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? SnapshotTimeText : Label;
}

/// <summary>
/// 快照中的单个文件夹大小明细
/// </summary>
public class SnapshotFolderItem
{
    /// <summary>明细 ID</summary>
    public int Id { get; set; }

    /// <summary>所属快照 ID</summary>
    public int SnapshotId { get; set; }

    /// <summary>文件夹路径</summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>文件夹名称（从路径提取）</summary>
    public string FolderName => Path.GetFileName(FolderPath) is { Length: > 0 } n ? n : FolderPath;

    /// <summary>文件夹大小（字节）</summary>
    public long SizeBytes { get; set; }

    /// <summary>文件夹大小格式化文字</summary>
    public string SizeBytesText => Helpers.FileSizeHelper.Format(SizeBytes);
}

/// <summary>
/// 两次快照的对比结果
/// </summary>
public class SnapshotDiffItem
{
    /// <summary>文件夹路径</summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>文件夹名称</summary>
    public string FolderName => Path.GetFileName(FolderPath) is { Length: > 0 } n ? n : FolderPath;

    /// <summary>旧快照中的大小（字节）</summary>
    public long OldSizeBytes { get; set; }

    /// <summary>新快照中的大小（字节）</summary>
    public long NewSizeBytes { get; set; }

    /// <summary>变化量（字节，正数为增加，负数为减少）</summary>
    public long DeltaBytes => NewSizeBytes - OldSizeBytes;

    /// <summary>变化量格式化文字（带符号）</summary>
    public string DeltaText
    {
        get
        {
            if (DeltaBytes == 0) return "无变化";
            string prefix = DeltaBytes > 0 ? "+" : "-";
            return $"{prefix}{Helpers.FileSizeHelper.Format(Math.Abs(DeltaBytes))}";
        }
    }

    /// <summary>旧大小格式化文字</summary>
    public string OldSizeText => Helpers.FileSizeHelper.Format(OldSizeBytes);

    /// <summary>新大小格式化文字</summary>
    public string NewSizeText => Helpers.FileSizeHelper.Format(NewSizeBytes);

    /// <summary>是否为新增文件夹（旧快照中不存在）</summary>
    public bool IsNew => OldSizeBytes == 0 && NewSizeBytes > 0;

    /// <summary>是否为已删除文件夹（新快照中不存在）</summary>
    public bool IsDeleted => OldSizeBytes > 0 && NewSizeBytes == 0;

    /// <summary>是否增长（变大了）</summary>
    public bool IsGrowing => DeltaBytes > 0;
}

namespace DiskSlim.Models;

/// <summary>
/// 磁盘信息模型，存储磁盘的空间使用情况
/// </summary>
public class DriveInfoModel
{
    /// <summary>盘符（如 "C:"）</summary>
    public string DriveLetter { get; set; } = string.Empty;

    /// <summary>磁盘卷标名称</summary>
    public string VolumeLabel { get; set; } = string.Empty;

    /// <summary>磁盘总容量（字节）</summary>
    public long TotalBytes { get; set; }

    /// <summary>已使用空间（字节）</summary>
    public long UsedBytes { get; set; }

    /// <summary>可用空间（字节）</summary>
    public long FreeBytes { get; set; }

    /// <summary>使用率（0.0 ~ 1.0）</summary>
    public double UsagePercent => TotalBytes > 0 ? (double)UsedBytes / TotalBytes : 0;

    /// <summary>文件系统类型（NTFS、FAT32 等）</summary>
    public string FileSystem { get; set; } = string.Empty;

    /// <summary>是否为系统盘</summary>
    public bool IsSystemDrive { get; set; }

    /// <summary>扫描时间</summary>
    public DateTime ScanTime { get; set; } = DateTime.Now;

    /// <summary>预估可释放空间（字节），由清理服务扫描后填充</summary>
    public long EstimatedFreeable { get; set; }
}

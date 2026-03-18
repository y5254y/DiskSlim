namespace DiskSlim.Models;

/// <summary>
/// 旧文件或临时文件的检测结果
/// </summary>
public class OldFileItem
{
    /// <summary>文件完整路径</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>文件名称</summary>
    public string Name => Path.GetFileName(FullPath);

    /// <summary>文件大小（字节）</summary>
    public long SizeBytes { get; set; }

    /// <summary>最后访问时间</summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>最后修改时间</summary>
    public DateTime LastModified { get; set; }

    /// <summary>文件扩展名（小写，含点号）</summary>
    public string Extension => Path.GetExtension(FullPath).ToLowerInvariant();

    /// <summary>检测类型：旧文件/临时文件/空文件/dump文件</summary>
    public OldFileType FileType { get; set; }

    /// <summary>是否被用户勾选（准备删除）</summary>
    public bool IsSelected { get; set; }

    /// <summary>大小格式化文字</summary>
    public string SizeBytesText => Helpers.FileSizeHelper.Format(SizeBytes);

    /// <summary>最后访问时间格式化文字</summary>
    public string LastAccessedText => LastAccessed.ToString("yyyy-MM-dd");

    /// <summary>距今未访问天数</summary>
    public int DaysSinceAccess => (int)(DateTime.Now - LastAccessed).TotalDays;

    /// <summary>未访问天数文字</summary>
    public string DaysSinceAccessText => $"{DaysSinceAccess} 天未访问";

    /// <summary>文件类型显示名称</summary>
    public string FileTypeName => FileType switch
    {
        OldFileType.OldFile => "旧文件",
        OldFileType.TempFile => "临时文件",
        OldFileType.EmptyFile => "空文件",
        OldFileType.DumpFile => "崩溃转储",
        _ => "其他"
    };
}

/// <summary>
/// 旧文件/临时文件类型枚举
/// </summary>
public enum OldFileType
{
    /// <summary>长期未访问的旧文件</summary>
    OldFile,
    /// <summary>临时文件（.tmp/.temp/.bak/.old/.log）</summary>
    TempFile,
    /// <summary>零字节空文件</summary>
    EmptyFile,
    /// <summary>崩溃转储文件（.dmp）</summary>
    DumpFile
}

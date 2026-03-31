namespace DupFinder.Models;

/// <summary>
/// 重复文件扫描选项
/// </summary>
public class ScanOptions
{
    /// <summary>要扫描的文件夹路径列表</summary>
    public List<string> ScanFolders { get; set; } = new();

    /// <summary>是否递归扫描子目录（默认 true）</summary>
    public bool IncludeSubdirectories { get; set; } = true;

    /// <summary>最小文件大小（字节），小于此值的文件跳过（默认 1 字节，即跳过空文件）</summary>
    public long MinFileSizeBytes { get; set; } = 1;

    /// <summary>文件类型过滤（空字符串表示所有类型）</summary>
    public string FileTypeFilter { get; set; } = string.Empty;

    /// <summary>是否跳过隐藏文件（默认 true）</summary>
    public bool SkipHiddenFiles { get; set; } = true;

    /// <summary>是否跳过系统文件（默认 true）</summary>
    public bool SkipSystemFiles { get; set; } = true;
}

/// <summary>
/// 扫描结果汇总
/// </summary>
public class ScanSummary
{
    /// <summary>扫描的文件总数</summary>
    public int TotalFilesScanned { get; set; }

    /// <summary>发现的重复文件组数</summary>
    public int DuplicateGroupCount { get; set; }

    /// <summary>重复文件总数（所有组的文件数之和）</summary>
    public int TotalDuplicateFiles { get; set; }

    /// <summary>可节省的总空间（字节）</summary>
    public long TotalWasteBytes { get; set; }
}

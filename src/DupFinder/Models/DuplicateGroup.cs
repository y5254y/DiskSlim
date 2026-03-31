using DupFinder.Helpers;
using System.Collections.ObjectModel;

namespace DupFinder.Models;

/// <summary>
/// 重复文件组：具有相同内容哈希的一组文件
/// </summary>
public class DuplicateGroup
{
    /// <summary>文件内容的哈希值（SHA256）</summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>每个文件的大小（字节）；同组内所有文件大小相同</summary>
    public long FileSize { get; set; }

    /// <summary>该组内的所有重复文件</summary>
    public ObservableCollection<DuplicateFileItem> Files { get; } = new();

    /// <summary>组内文件数量</summary>
    public int Count => Files.Count;

    /// <summary>可节省的冗余空间（字节）：(n-1) 个副本 × 文件大小</summary>
    public long WasteBytes => FileSize * Math.Max(0, Files.Count - 1);

    /// <summary>可节省空间的格式化文字</summary>
    public string WasteBytesText => FileSizeHelper.Format(WasteBytes);

    /// <summary>单文件大小的格式化文字</summary>
    public string FileSizeText => FileSizeHelper.Format(FileSize);

    /// <summary>哈希缩略（前8位 + …）用于界面显示</summary>
    public string HashShort => Hash.Length > 8 ? Hash[..8] + "…" : Hash;

    /// <summary>组标题摘要（如 "3 个重复文件 · 每个 2.5 MB · 可节省 5.0 MB"）</summary>
    public string GroupSummary =>
        $"{Count} 个重复文件  ·  每个 {FileSizeText}  ·  可节省 {WasteBytesText}";
}

using CommunityToolkit.Mvvm.ComponentModel;
using DupFinder.Helpers;

namespace DupFinder.Models;

/// <summary>
/// 重复文件组内的单个文件条目
/// </summary>
public partial class DuplicateFileItem : ObservableObject
{
    /// <summary>文件完整路径</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>文件名称</summary>
    public string Name => Path.GetFileName(FullPath);

    /// <summary>所在目录</summary>
    public string Directory => Path.GetDirectoryName(FullPath) ?? string.Empty;

    /// <summary>文件大小（字节）</summary>
    public long SizeBytes { get; set; }

    /// <summary>文件最后修改时间</summary>
    public DateTime LastModified { get; set; }

    /// <summary>文件扩展名（小写，含点号，如 ".jpg"）</summary>
    public string Extension => Path.GetExtension(FullPath).ToLowerInvariant();

    /// <summary>是否被用户勾选（准备删除）</summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>是否被标记为保留的源文件（高亮显示，不会被选中删除）</summary>
    [ObservableProperty]
    private bool _isKeeper;

    /// <summary>大小格式化文字</summary>
    public string SizeBytesText => FileSizeHelper.Format(SizeBytes);

    /// <summary>最后修改时间格式化文字</summary>
    public string LastModifiedText => LastModified.ToString("yyyy-MM-dd HH:mm");
}

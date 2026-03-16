namespace DiskSlim.Models;

/// <summary>
/// 文件信息模型，用于大文件排行等列表显示
/// </summary>
public class FileItem
{
    /// <summary>文件或文件夹的完整路径</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>文件或文件夹名称</summary>
    public string Name => Path.GetFileName(FullPath) is { Length: > 0 } n ? n : FullPath;

    /// <summary>文件大小（字节）；如果是文件夹，则为递归总大小</summary>
    public long SizeBytes { get; set; }

    /// <summary>最后修改时间</summary>
    public DateTime LastModified { get; set; }

    /// <summary>最后访问时间</summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>是否为文件夹</summary>
    public bool IsDirectory { get; set; }

    /// <summary>文件扩展名（小写，含点号，如 ".mp4"）</summary>
    public string Extension => IsDirectory ? string.Empty : Path.GetExtension(FullPath).ToLowerInvariant();

    /// <summary>文件类型分类（视频/图片/文档/音乐/压缩包/代码/其他）</summary>
    public string FileCategory => GetFileCategory(Extension);

    /// <summary>占父级或整盘的百分比（0.0 ~ 1.0），由调用方填充</summary>
    public double SizePercent { get; set; }

    /// <summary>子项数量（仅文件夹有意义）</summary>
    public int ChildCount { get; set; }

    /// <summary>
    /// 根据扩展名判断文件类型分类
    /// </summary>
    private static string GetFileCategory(string ext) => ext switch
    {
        ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" or ".m4v" => "视频",
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".heic" or ".tiff" => "图片",
        ".mp3" or ".flac" or ".aac" or ".wav" or ".ogg" or ".m4a" or ".wma" => "音乐",
        ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" => "文档",
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".xz" => "压缩包",
        ".exe" or ".msi" or ".dll" or ".sys" => "程序",
        ".cs" or ".py" or ".js" or ".ts" or ".java" or ".cpp" or ".c" or ".go" => "代码",
        _ => "其他"
    };
}

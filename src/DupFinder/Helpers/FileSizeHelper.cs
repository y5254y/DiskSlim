namespace DupFinder.Helpers;

/// <summary>
/// 文件大小格式化工具类，自动将字节数转换为人类可读的格式
/// </summary>
public static class FileSizeHelper
{
    private const long KB = 1024L;
    private const long MB = 1024L * KB;
    private const long GB = 1024L * MB;
    private const long TB = 1024L * GB;

    /// <summary>
    /// 将字节数格式化为人类可读的字符串（B/KB/MB/GB/TB）
    /// </summary>
    public static string Format(long bytes, int decimals = 2)
    {
        if (bytes < 0) return "0 B";
        if (bytes < KB) return $"{bytes} B";
        if (bytes < MB) return $"{((double)bytes / KB).ToString($"F{decimals}")} KB";
        if (bytes < GB) return $"{((double)bytes / MB).ToString($"F{decimals}")} MB";
        if (bytes < TB) return $"{((double)bytes / GB).ToString($"F{decimals}")} GB";
        return $"{((double)bytes / TB).ToString($"F{decimals}")} TB";
    }

    /// <summary>
    /// 将字节数格式化为简短字符串（不含小数的粗略值）
    /// </summary>
    public static string FormatShort(long bytes)
    {
        if (bytes < KB) return $"{bytes} B";
        if (bytes < MB) return $"{bytes / KB} KB";
        if (bytes < GB) return $"{bytes / MB} MB";
        if (bytes < TB) return $"{bytes / GB} GB";
        return $"{bytes / TB} TB";
    }

    /// <summary>
    /// 获取占比百分比字符串
    /// </summary>
    public static string FormatPercent(long part, long total)
    {
        if (total <= 0) return "0%";
        double percent = (double)part / total * 100.0;
        return $"{percent:F1}%";
    }
}

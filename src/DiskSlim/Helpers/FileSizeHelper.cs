namespace DiskSlim.Helpers;

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
    /// <param name="bytes">字节数</param>
    /// <param name="decimals">小数位数，默认2位</param>
    /// <returns>格式化后的字符串，如 "1.23 GB"</returns>
    public static string Format(long bytes, int decimals = 2)
    {
        if (bytes < 0) return "0 B";
        if (bytes < KB) return $"{bytes} B";
        if (bytes < MB) return $"{(double)bytes / KB:F{decimals}} KB";
        if (bytes < GB) return $"{(double)bytes / MB:F{decimals}} MB";
        if (bytes < TB) return $"{(double)bytes / GB:F{decimals}} GB";
        return $"{(double)bytes / TB:F{decimals}} TB";
    }

    /// <summary>
    /// 将字节数格式化为简短字符串（不含小数的粗略值）
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>简短格式，如 "1 GB"</returns>
    public static string FormatShort(long bytes)
    {
        if (bytes < KB) return $"{bytes} B";
        if (bytes < MB) return $"{bytes / KB} KB";
        if (bytes < GB) return $"{bytes / MB} MB";
        if (bytes < TB) return $"{bytes / GB} GB";
        return $"{bytes / TB} TB";
    }

    /// <summary>
    /// 将字节数转换为GB，返回double类型
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>GB 数值</returns>
    public static double ToGB(long bytes) => (double)bytes / GB;

    /// <summary>
    /// 将字节数转换为MB，返回double类型
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>MB 数值</returns>
    public static double ToMB(long bytes) => (double)bytes / MB;

    /// <summary>
    /// 获取占比百分比字符串
    /// </summary>
    /// <param name="part">分子字节数</param>
    /// <param name="total">分母字节数</param>
    /// <returns>百分比字符串，如 "72.5%"</returns>
    public static string FormatPercent(long part, long total)
    {
        if (total <= 0) return "0%";
        double percent = (double)part / total * 100.0;
        return $"{percent:F1}%";
    }
}

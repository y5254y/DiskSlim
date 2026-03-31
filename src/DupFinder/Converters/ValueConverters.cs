using DupFinder.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DupFinder.Converters;

/// <summary>
/// 将文件大小（字节）转换为可读字符串（KB/MB/GB）
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long bytes) return FileSizeHelper.Format(bytes);
        if (value is int intBytes) return FileSizeHelper.Format(intBytes);
        return "--";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// bool 转 Visibility（true → Visible，false → Collapsed）
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>
/// bool 取反 Visibility（true → Collapsed，false → Visible）
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v == Visibility.Collapsed;
}

/// <summary>
/// bool 取反（用于 IsEnabled 绑定）
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : true;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is bool b ? !b : false;
}

/// <summary>
/// null → Collapsed，非 null → Visible
/// </summary>
public class NullToInvisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// int 大于0 → Visible，否则 Collapsed
/// </summary>
public class GreaterThanZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int i) return i > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (value is long l) return l > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// bool → 成功/失败颜色（绿色/红色）
/// </summary>
public class BoolToSuccessColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return b
                ? new SolidColorBrush(Color.FromArgb(255, 23, 195, 70))
                : new SolidColorBrush(Color.FromArgb(255, 232, 77, 80));
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// 将文件扩展名转换为 Segoe MDL2 图标字形
/// </summary>
public class ExtensionToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var ext = (value as string ?? string.Empty).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".heic" => "\uEB9F", // 图片
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" => "\uE8B2",             // 视频
            ".mp3" or ".flac" or ".aac" or ".wav" or ".ogg" or ".m4a" => "\uEC4F",            // 音乐
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => "\uE8A5", // 文档
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "\uE7B8",                          // 压缩包
            ".exe" or ".msi" or ".dll" => "\uE756",                                             // 程序
            _ => "\uE7C3"                                                                       // 通用文件
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

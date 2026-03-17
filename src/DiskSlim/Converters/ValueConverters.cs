using DiskSlim.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DiskSlim.Converters;

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
/// 根据安全等级返回背景颜色 Brush
/// </summary>
public class SafetyLevelToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is SafetyLevel level)
        {
            return level switch
            {
                SafetyLevel.Safe => new SolidColorBrush(Color.FromArgb(40, 23, 195, 70)),
                SafetyLevel.Caution => new SolidColorBrush(Color.FromArgb(40, 255, 168, 0)),
                SafetyLevel.Danger => new SolidColorBrush(Color.FromArgb(40, 232, 77, 80)),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// 将 bool (IsDirectory) 转换为文件夹/文件图标字形
/// </summary>
public class BoolToFolderIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool isDir && isDir ? "\uE8B7" : "\uE7C3"; // 文件夹/文件图标

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// long 大于0 → Visible，否则 Collapsed
/// </summary>
public class GreaterThanZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is long l && l > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
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
/// null → Visible，非 null → Collapsed（用于"未选中"占位显示）
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value == null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>
/// bool → 成功/失败图标字形（✓/✗）
/// </summary>
public class BoolToSuccessIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? "\uE73E" : "\uE711"; // 对勾 / X号

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
                ? new SolidColorBrush(Color.FromArgb(255, 23, 195, 70))  // 绿色
                : new SolidColorBrush(Color.FromArgb(255, 232, 77, 80)); // 红色
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

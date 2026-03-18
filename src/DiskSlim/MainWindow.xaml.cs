using DiskSlim.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace DiskSlim;

/// <summary>
/// 主窗口，使用 NavigationView 实现左侧导航菜单，支持深色/浅色主题切换
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// 当前主题模式（深色/浅色）
    /// </summary>
    private ElementTheme _currentTheme = ElementTheme.Default;

    /// <summary>
    /// 内容导航框架，供页面间导航使用
    /// </summary>
    public Frame NavigationFrame => ContentFrame;

    public MainWindow()
    {
        this.InitializeComponent();
        SetupWindow();

        // 默认导航到仪表盘页面
        NavView.SelectedItem = NavDashboard;
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    /// <summary>
    /// 初始化窗口大小和标题栏
    /// </summary>
    private void SetupWindow()
    {
        // 设置窗口最小尺寸和初始尺寸
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new SizeInt32(1200, 780));
        appWindow.Title = "DiskSlim - C盘瘦身大师";
    }

    /// <summary>
    /// 导航菜单选中项变化时，切换到对应页面
    /// </summary>
    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            Type? pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Cleanup" => typeof(CleanupPage),
                "Migration" => typeof(MigrationPage),
                "SoftwareMove" => typeof(SoftwareMovePage),
                "CleanupReport" => typeof(CleanupReportPage),
                // Phase 3 新增页面
                "Snapshot" => typeof(SnapshotPage),
                "Trend" => typeof(TrendPage),
                "OldFiles" => typeof(OldFilesPage),
                "Settings" => typeof(SettingsPage),
                _ => null
            };

            if (pageType != null)
            {
                PageTitle.Text = item.Content?.ToString() ?? string.Empty;
                ContentFrame.Navigate(pageType);
            }
        }
    }

    /// <summary>
    /// 点击底部"切换主题"时，在深色和浅色之间切换
    /// </summary>
    private void ThemeToggle_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (Content is FrameworkElement root)
        {
            if (_currentTheme == ElementTheme.Light || _currentTheme == ElementTheme.Default)
            {
                _currentTheme = ElementTheme.Dark;
                root.RequestedTheme = ElementTheme.Dark;
                ThemeIcon.Glyph = "\uE706"; // 太阳图标（切换回浅色）
            }
            else
            {
                _currentTheme = ElementTheme.Light;
                root.RequestedTheme = ElementTheme.Light;
                ThemeIcon.Glyph = "\uE793"; // 月亮图标（切换回深色）
            }
        }
    }

    /// <summary>
    /// 导航到指定页面（供其他页面调用）
    /// </summary>
    public void NavigateTo(Type pageType)
    {
        ContentFrame.Navigate(pageType);
    }
}

using DiskSlim.Helpers;
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
    private bool _permissionHintShown;

    /// <summary>
    /// 内容导航框架，供页面间导航使用
    /// </summary>
    public Frame NavigationFrame => ContentFrame;

    public MainWindow()
    {
        this.InitializeComponent();
        SetupWindow();

        this.Activated += MainWindow_Activated;

        // 默认导航到仪表盘页面
        NavView.SelectedItem = NavDashboard;
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_permissionHintShown || args.WindowActivationState == WindowActivationState.Deactivated)
            return;

        _permissionHintShown = true;
        if (AdminHelper.IsRunningAsAdmin())
            return;

        if (Content is not FrameworkElement root)
            return;

        var dialog = new ContentDialog
        {
            Title = "当前为普通权限模式",
            Content = "DiskSlim 已正常启动。涉及系统目录、服务或休眠设置的操作可能需要管理员权限。你可以继续使用，或现在提权后重启。",
            PrimaryButtonText = "继续使用",
            SecondaryButtonText = "提权后重启",
            CloseButtonText = "关闭",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = root.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Secondary)
            AdminHelper.RestartAsAdmin();
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
                // Phase 4 新增页面
                "CompactOs" => typeof(CompactOsPage),
                "Wsl" => typeof(WslPage),
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
    /// 导航到指定页面（供其他页面调用）
    /// </summary>
    public void NavigateTo(Type pageType)
    {
        ContentFrame.Navigate(pageType);
    }
}

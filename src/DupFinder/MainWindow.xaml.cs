using DupFinder.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace DupFinder;

/// <summary>
/// 主窗口，使用 NavigationView 实现左侧导航菜单
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// 内容导航框架，供页面间导航使用
    /// </summary>
    public Frame NavigationFrame => ContentFrame;

    public MainWindow()
    {
        this.InitializeComponent();
        SetupWindow();

        // 默认导航到扫描页面
        NavView.SelectedItem = NavScan;
        ContentFrame.Navigate(typeof(ScanPage));
    }

    /// <summary>
    /// 初始化窗口大小和标题
    /// </summary>
    private void SetupWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new SizeInt32(1100, 740));
        appWindow.Title = "DupFinder - 重复文件清理";
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
                "Scan" => typeof(ScanPage),
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
}

using DiskSlim.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace DiskSlim.Views;

/// <summary>
/// 仪表盘页面，展示C盘空间使用概况和大文件排行
/// </summary>
public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
        this.Loaded += async (s, e) =>
        {
            try
            {
                await ViewModel.RefreshCommand.ExecuteAsync(null);
            }
            catch
            {
                // 启动时刷新失败不影响页面正常显示
            }
        };
    }

    /// <summary>
    /// 点击"一键智能清理"按钮，导航到清理页面
    /// </summary>
    private void QuickClean_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (App.MainWindow is MainWindow mainWindow)
        {
            mainWindow.NavigateTo(typeof(CleanupPage));
        }
    }
}

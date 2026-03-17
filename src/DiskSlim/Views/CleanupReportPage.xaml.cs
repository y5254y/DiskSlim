using DiskSlim.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DiskSlim.Views;

/// <summary>
/// 清理历史报告页面，显示过往清理记录并支持导出
/// </summary>
public sealed partial class CleanupReportPage : Page
{
    public CleanupReportViewModel ViewModel { get; }

    public CleanupReportPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CleanupReportViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // 每次导航到此页面时自动加载最新记录
        await ViewModel.LoadReportsAsync();
    }
}

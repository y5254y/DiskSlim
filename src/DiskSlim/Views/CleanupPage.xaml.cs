using DiskSlim.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DiskSlim.Views;

/// <summary>
/// 智能清理页面，显示可清理项目列表并执行清理
/// </summary>
public sealed partial class CleanupPage : Page
{
    public CleanupViewModel ViewModel { get; }

    public CleanupPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<CleanupViewModel>();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // 将 XamlRoot 传递给 ViewModel，以便弹出确认对话框
        ViewModel.XamlRoot = this.XamlRoot;
    }
}

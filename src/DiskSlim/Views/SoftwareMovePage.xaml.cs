using DiskSlim.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace DiskSlim.Views;

/// <summary>
/// 软件搬家页面，将安装在C盘的软件迁移到其他磁盘（通过符号链接）
/// </summary>
public sealed partial class SoftwareMovePage : Page
{
    public SoftwareMoveViewModel ViewModel { get; }

    public SoftwareMovePage()
    {
        this.InitializeComponent();
        NavigationCacheMode = NavigationCacheMode.Enabled;
        ViewModel = App.Services.GetRequiredService<SoftwareMoveViewModel>();
    }
}

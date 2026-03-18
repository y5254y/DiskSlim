using DiskSlim.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DiskSlim.Views;

/// <summary>
/// 设置页面，配置定时扫描、通知和主题等选项
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        this.InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadSettingsAsync();
    }
}

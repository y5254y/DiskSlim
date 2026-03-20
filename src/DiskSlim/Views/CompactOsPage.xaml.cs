using DiskSlim.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace DiskSlim.Views;

/// <summary>
/// CompactOS 系统压缩页面
/// </summary>
public sealed partial class CompactOsPage : Page
{
    public CompactOsViewModel ViewModel { get; }

    public CompactOsPage()
    {
        ViewModel = App.Services.GetRequiredService<CompactOsViewModel>();
        this.InitializeComponent();
    }
}

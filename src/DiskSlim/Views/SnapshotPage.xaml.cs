using DiskSlim.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace DiskSlim.Views;

/// <summary>
/// 历史快照页面，管理磁盘扫描快照的保存、查看和对比
/// </summary>
public sealed partial class SnapshotPage : Page
{
    public SnapshotViewModel ViewModel { get; }

    public SnapshotPage()
    {
        ViewModel = App.Services.GetRequiredService<SnapshotViewModel>();
        this.InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadSnapshotsAsync();
    }
}

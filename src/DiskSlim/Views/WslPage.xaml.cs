using DiskSlim.Models;
using DiskSlim.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace DiskSlim.Views;

/// <summary>
/// WSL 磁盘空间回收页面
/// </summary>
public sealed partial class WslPage : Page
{
    public WslViewModel ViewModel { get; }

    public WslPage()
    {
        ViewModel = App.Services.GetRequiredService<WslViewModel>();
        this.InitializeComponent();
    }

    /// <summary>
    /// 发行版列表中"回收空间"按钮点击处理
    /// （DataTemplate 内无法直接绑定父级 ViewModel 命令，通过 Click 事件中转）
    /// </summary>
    private void ReclaimButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WslDistribution distribution)
        {
            ViewModel.ReclaimCommand.Execute(distribution);
        }
    }
}

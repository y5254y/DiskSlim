using DiskSlim.Models;
using DiskSlim.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DiskSlim.Views;

/// <summary>
/// 旧文件/临时文件检测页面
/// </summary>
public sealed partial class OldFilesPage : Page
{
    public OldFilesViewModel ViewModel { get; }

    public OldFilesPage()
    {
        ViewModel = App.Services.GetRequiredService<OldFilesViewModel>();
        this.InitializeComponent();
    }

    /// <summary>
    /// 点击"在资源管理器中打开"按钮，传递文件路径给 ViewModel 命令
    /// </summary>
    private void OpenInExplorerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            var item = new OldFileItem { FullPath = path };
            ViewModel.OpenInExplorerCommand.Execute(item);
        }
    }
}

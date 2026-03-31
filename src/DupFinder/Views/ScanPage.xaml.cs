using DupFinder.Models;
using DupFinder.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace DupFinder.Views;

/// <summary>
/// 重复文件扫描与清理页面
/// </summary>
public sealed partial class ScanPage : Page
{
    public ScanViewModel ViewModel { get; }

    public ScanPage()
    {
        ViewModel = App.Services.GetRequiredService<ScanViewModel>();
        this.InitializeComponent();
    }

    /// <summary>
    /// 点击"在资源管理器中打开"按钮
    /// </summary>
    private void OpenInExplorer_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DuplicateFileItem item)
            ViewModel.OpenInExplorerCommand.Execute(item);
    }
}

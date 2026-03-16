using DiskSlim.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace DiskSlim.Views;

/// <summary>
/// 文件夹迁移页面，将用户文件夹从C盘迁移到其他磁盘
/// </summary>
public sealed partial class MigrationPage : Page
{
    public MigrationViewModel ViewModel { get; }

    public MigrationPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<MigrationViewModel>();
    }
}

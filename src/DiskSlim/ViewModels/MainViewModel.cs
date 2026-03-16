using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Services;

namespace DiskSlim.ViewModels;

/// <summary>
/// 主页面 ViewModel，负责全局状态管理（如管理员权限检测）
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IDiskScanService _diskScanService;

    [ObservableProperty]
    private bool _isAdminMode;

    [ObservableProperty]
    private string _permissionStatus = "检测中...";

    public MainViewModel(IDiskScanService diskScanService)
    {
        _diskScanService = diskScanService;
        CheckAdminStatus();
    }

    /// <summary>
    /// 检测当前运行的权限状态
    /// </summary>
    private void CheckAdminStatus()
    {
        IsAdminMode = Helpers.AdminHelper.IsRunningAsAdmin();
        PermissionStatus = Helpers.AdminHelper.GetPermissionStatus();
    }
}

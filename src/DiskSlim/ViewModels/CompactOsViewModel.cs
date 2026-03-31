using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Helpers;
using DiskSlim.Services;

namespace DiskSlim.ViewModels;

/// <summary>
/// CompactOS 系统压缩页面 ViewModel
/// </summary>
public partial class CompactOsViewModel : ObservableObject
{
    private readonly ICompactOsService _compactOsService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCompressed;

    [ObservableProperty]
    private string _statusMessage = Localizer.Get("Vm.CompactOs.NotQueried", "尚未查询状态");

    [ObservableProperty]
    private string _estimatedSavings = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutputLog))]
    private string _outputLog = string.Empty;

    [ObservableProperty]
    private bool _hasStatus;

    /// <summary>是否有日志内容可显示</summary>
    public bool HasOutputLog => !string.IsNullOrWhiteSpace(_outputLog);

    public CompactOsViewModel(ICompactOsService compactOsService)
    {
        _compactOsService = compactOsService;
    }

    /// <summary>查询当前 CompactOS 状态</summary>
    [RelayCommand]
    public async Task LoadStatusAsync()
    {
        IsLoading = true;
        StatusMessage = Localizer.Get("Vm.CompactOs.Querying", "正在查询 CompactOS 状态…");
        OutputLog = string.Empty;

        try
        {
            var status = await _compactOsService.GetStatusAsync();

            if (status.IsSuccess)
            {
                IsCompressed = status.IsCompressed;
                StatusMessage = status.IsCompressed
                    ? Localizer.Get("Vm.CompactOs.Enabled", "✅ 系统已启用 CompactOS 压缩")
                    : Localizer.Get("Vm.CompactOs.Disabled", "ℹ️ 系统未启用 CompactOS 压缩");
                EstimatedSavings = status.IsCompressed
                    ? Localizer.Get("Vm.CompactOs.SavedHint", "已节省约 1.5 GB 磁盘空间")
                    : Localizer.Get("Vm.CompactOs.CanSaveHint", "启用后可节省约 1–3 GB 磁盘空间");
                HasStatus = true;
                OutputLog = status.RawOutput;
            }
            else
            {
                StatusMessage = Localizer.Format("Vm.CompactOs.QueryFailed", "查询失败：{0}", status.ErrorMessage);
                HasStatus = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.CompactOs.QueryError", "查询出错：{0}", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>启用 CompactOS 系统压缩（需要管理员权限）</summary>
    [RelayCommand]
    public async Task EnableAsync()
    {
        IsLoading = true;
        OutputLog = string.Empty;
        StatusMessage = Localizer.Get("Vm.CompactOs.Enabling", "正在启用压缩，请勿关闭窗口…");

        var progress = new Progress<string>(msg =>
        {
            StatusMessage = msg;
            OutputLog += msg + Environment.NewLine;
        });

        try
        {
            var result = await _compactOsService.EnableCompactionAsync(progress);

            if (result.IsSuccess)
            {
                IsCompressed = true;
                StatusMessage = Localizer.Get("Vm.CompactOs.EnableDone", "✅ CompactOS 压缩已成功启用！");
                EstimatedSavings = Localizer.Get("Vm.CompactOs.EnableSaved", "已节省约 1–3 GB 磁盘空间");
                OutputLog += result.Output;
            }
            else
            {
                StatusMessage = Localizer.Format("Vm.CompactOs.EnableFailed", "❌ 启用失败：{0}", result.ErrorMessage);
                OutputLog += Environment.NewLine + Localizer.Format("Vm.CompactOs.ErrorPrefix", "错误：{0}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.CompactOs.EnableError", "启用出错：{0}", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>禁用 CompactOS 系统压缩（需要管理员权限）</summary>
    [RelayCommand]
    public async Task DisableAsync()
    {
        IsLoading = true;
        OutputLog = string.Empty;
        StatusMessage = Localizer.Get("Vm.CompactOs.Disabling", "正在禁用压缩，请勿关闭窗口…");

        var progress = new Progress<string>(msg =>
        {
            StatusMessage = msg;
            OutputLog += msg + Environment.NewLine;
        });

        try
        {
            var result = await _compactOsService.DisableCompactionAsync(progress);

            if (result.IsSuccess)
            {
                IsCompressed = false;
                StatusMessage = Localizer.Get("Vm.CompactOs.DisableDone", "ℹ️ CompactOS 压缩已禁用");
                EstimatedSavings = Localizer.Get("Vm.CompactOs.CanSaveHint", "启用后可节省约 1–3 GB 磁盘空间");
                OutputLog += result.Output;
            }
            else
            {
                StatusMessage = Localizer.Format("Vm.CompactOs.DisableFailed", "❌ 禁用失败：{0}", result.ErrorMessage);
                OutputLog += Environment.NewLine + Localizer.Format("Vm.CompactOs.ErrorPrefix", "错误：{0}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.CompactOs.DisableError", "禁用出错：{0}", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private string _statusMessage = "尚未查询状态";

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
        StatusMessage = "正在查询 CompactOS 状态…";
        OutputLog = string.Empty;

        try
        {
            var status = await _compactOsService.GetStatusAsync();

            if (status.IsSuccess)
            {
                IsCompressed = status.IsCompressed;
                StatusMessage = status.IsCompressed
                    ? "✅ 系统已启用 CompactOS 压缩"
                    : "ℹ️ 系统未启用 CompactOS 压缩";
                EstimatedSavings = status.IsCompressed
                    ? "已节省约 1.5 GB 磁盘空间"
                    : "启用后可节省约 1–3 GB 磁盘空间";
                HasStatus = true;
                OutputLog = status.RawOutput;
            }
            else
            {
                StatusMessage = $"查询失败：{status.ErrorMessage}";
                HasStatus = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"查询出错：{ex.Message}";
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
        StatusMessage = "正在启用压缩，请勿关闭窗口…";

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
                StatusMessage = "✅ CompactOS 压缩已成功启用！";
                EstimatedSavings = "已节省约 1–3 GB 磁盘空间";
                OutputLog += result.Output;
            }
            else
            {
                StatusMessage = $"❌ 启用失败：{result.ErrorMessage}";
                OutputLog += $"\n错误：{result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"启用出错：{ex.Message}";
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
        StatusMessage = "正在禁用压缩，请勿关闭窗口…";

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
                StatusMessage = "ℹ️ CompactOS 压缩已禁用";
                EstimatedSavings = "启用后可节省约 1–3 GB 磁盘空间";
                OutputLog += result.Output;
            }
            else
            {
                StatusMessage = $"❌ 禁用失败：{result.ErrorMessage}";
                OutputLog += $"\n错误：{result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"禁用出错：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Helpers;
using DiskSlim.Models;
using DiskSlim.Services;

namespace DiskSlim.ViewModels;

/// <summary>
/// WSL 磁盘空间回收页面 ViewModel
/// </summary>
public partial class WslViewModel : ObservableObject
{
    private readonly IWslService _wslService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isReclaiming;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWslNotInstalled))]
    private bool _isWslInstalled;

    [ObservableProperty]
    private string _statusMessage = "点击"扫描"检测 WSL 发行版";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutputLog))]
    private string _outputLog = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSaved))]
    private long _totalSavedBytes;

    /// <summary>是否有发行版可显示</summary>
    public bool HasDistributions => Distributions.Count > 0;

    /// <summary>WSL 未安装标志（供 InfoBar 绑定）</summary>
    public bool IsWslNotInstalled => !IsWslInstalled;

    /// <summary>是否有日志内容可显示</summary>
    public bool HasOutputLog => !string.IsNullOrWhiteSpace(_outputLog);

    /// <summary>是否有节省空间数据可显示</summary>
    public bool HasSaved => _totalSavedBytes > 0;

    /// <summary>已检测到的 WSL 发行版列表</summary>
    public ObservableCollection<WslDistribution> Distributions { get; } = [];

    public WslViewModel(IWslService wslService)
    {
        _wslService = wslService;
        Distributions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasDistributions));
    }

    /// <summary>扫描 WSL 发行版及其 vhdx 文件大小</summary>
    [RelayCommand]
    public async Task ScanAsync()
    {
        IsLoading = true;
        StatusMessage = "正在扫描 WSL 发行版…";
        Distributions.Clear();
        OutputLog = string.Empty;

        try
        {
            IsWslInstalled = await _wslService.IsWslInstalledAsync();

            if (!IsWslInstalled)
            {
                StatusMessage = "未检测到 WSL，请先安装 Windows Subsystem for Linux";
                return;
            }

            var distros = await _wslService.GetDistributionsAsync();

            foreach (var d in distros)
                Distributions.Add(d);

            if (Distributions.Count == 0)
            {
                StatusMessage = "未找到任何 WSL 发行版";
            }
            else
            {
                long totalVhdx = Distributions.Sum(d => d.VhdxSizeBytes);
                StatusMessage = $"找到 {Distributions.Count} 个发行版，虚拟磁盘总计 {FileSizeHelper.Format(totalVhdx)}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"扫描出错：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>回收指定发行版的 WSL 虚拟磁盘空间</summary>
    [RelayCommand]
    public async Task ReclaimAsync(WslDistribution distribution)
    {
        IsReclaiming = true;
        OutputLog = string.Empty;
        StatusMessage = $"正在处理 {distribution.Name}，请勿关闭窗口…";

        var progress = new Progress<string>(msg =>
        {
            StatusMessage = msg;
            OutputLog += msg + Environment.NewLine;
        });

        try
        {
            var result = await _wslService.ReclaimDiskSpaceAsync(distribution, progress);

            OutputLog += result.Output;

            if (result.IsSuccess)
            {
                TotalSavedBytes += result.SavedBytes;
                StatusMessage = result.SavedBytes > 0
                    ? $"✅ {distribution.Name} 完成！释放了 {FileSizeHelper.Format(result.SavedBytes)}"
                    : $"✅ {distribution.Name} 压缩完成（空间已是最优）";

                // 更新列表中该发行版的 vhdx 大小
                distribution.VhdxSizeBytes = result.SizeAfterBytes;
            }
            else
            {
                StatusMessage = $"❌ {distribution.Name} 失败：{result.ErrorMessage}";
                OutputLog += $"\n错误：{result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"处理出错：{ex.Message}";
        }
        finally
        {
            IsReclaiming = false;
        }
    }

    /// <summary>回收所有发行版的 WSL 虚拟磁盘空间</summary>
    [RelayCommand]
    public async Task ReclaimAllAsync()
    {
        if (Distributions.Count == 0) return;

        IsReclaiming = true;
        OutputLog = string.Empty;
        TotalSavedBytes = 0;

        var progress = new Progress<string>(msg =>
        {
            StatusMessage = msg;
            OutputLog += msg + Environment.NewLine;
        });

        try
        {
            foreach (var dist in Distributions.Where(d => d.VhdxFound))
            {
                var result = await _wslService.ReclaimDiskSpaceAsync(dist, progress);
                OutputLog += result.Output + Environment.NewLine;

                if (result.IsSuccess)
                {
                    TotalSavedBytes += result.SavedBytes;
                    dist.VhdxSizeBytes = result.SizeAfterBytes;
                }
            }

            StatusMessage = TotalSavedBytes > 0
                ? $"✅ 全部完成！共释放 {FileSizeHelper.Format(TotalSavedBytes)}"
                : "✅ 全部压缩完成（空间已是最优）";
        }
        catch (Exception ex)
        {
            StatusMessage = $"处理出错：{ex.Message}";
        }
        finally
        {
            IsReclaiming = false;
        }
    }
}

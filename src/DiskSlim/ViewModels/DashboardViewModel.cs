using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Helpers;
using DiskSlim.Models;
using DiskSlim.Services;
using System.Collections.ObjectModel;

namespace DiskSlim.ViewModels;

/// <summary>
/// 仪表盘 ViewModel，显示C盘空间使用概况（已用/可用/总计）和可释放空间预估
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly IDiskScanService _diskScanService;
    private readonly ICleanupService _cleanupService;

    // ===== 磁盘基础信息 =====

    [ObservableProperty]
    private string _driveLetter = "C:";

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private long _usedBytes;

    [ObservableProperty]
    private long _freeBytes;

    [ObservableProperty]
    private double _usagePercent;

    [ObservableProperty]
    private string _totalSizeText = "--";

    [ObservableProperty]
    private string _usedSizeText = "--";

    [ObservableProperty]
    private string _freeSizeText = "--";

    [ObservableProperty]
    private string _usagePercentText = "--";

    [ObservableProperty]
    private string _estimatedFreeableText = "--";

    [ObservableProperty]
    private long _estimatedFreeableBytes;

    // ===== 状态标志 =====

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private string _statusMessage = "点击刷新查看C盘使用情况";

    // ===== Top 文件列表 =====
    public ObservableCollection<FileItem> TopItems { get; } = new();

    public DashboardViewModel(IDiskScanService diskScanService, ICleanupService cleanupService)
    {
        _diskScanService = diskScanService;
        _cleanupService = cleanupService;
    }

    /// <summary>
    /// 刷新C盘空间信息
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        StatusMessage = "正在扫描C盘...";

        try
        {
            // 获取磁盘基础信息
            var driveModel = await _diskScanService.ScanDriveAsync("C:\\");

            TotalBytes = driveModel.TotalBytes;
            UsedBytes = driveModel.UsedBytes;
            FreeBytes = driveModel.FreeBytes;
            UsagePercent = driveModel.UsagePercent * 100.0;

            TotalSizeText = FileSizeHelper.Format(TotalBytes);
            UsedSizeText = FileSizeHelper.Format(UsedBytes);
            FreeSizeText = FileSizeHelper.Format(FreeBytes);
            UsagePercentText = $"{UsagePercent:F1}%";

            // 扫描可释放空间估算
            StatusMessage = "正在估算可释放空间...";
            var cleanupItems = _cleanupService.GetCleanupItems();
            await _cleanupService.ScanEstimatedSizesAsync(cleanupItems);
            EstimatedFreeableBytes = cleanupItems
                .Where(i => i.Safety == Helpers.SafetyLevel.Safe)
                .Sum(i => i.EstimatedSize);
            EstimatedFreeableText = FileSizeHelper.Format(EstimatedFreeableBytes);

            // 扫描 Top 大文件
            StatusMessage = "正在扫描大文件...";
            var topItems = await _diskScanService.ScanTopItemsAsync("C:\\", maxItems: 10);
            TopItems.Clear();
            foreach (var item in topItems)
                TopItems.Add(item);

            IsLoaded = true;
            StatusMessage = $"扫描完成 · {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"扫描出错：{ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }
}

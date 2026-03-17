using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Helpers;
using DiskSlim.Models;
using DiskSlim.Services;
using System.Collections.ObjectModel;

namespace DiskSlim.ViewModels;

/// <summary>
/// 智能清理 ViewModel，管理清理项目的扫描、选择和执行
/// </summary>
public partial class CleanupViewModel : ObservableObject
{
    private readonly ICleanupService _cleanupService;
    private CancellationTokenSource? _cts;

    // ===== 清理项目列表 =====
    public ObservableCollection<CleanupItem> CleanupItems { get; } = new();

    // ===== 统计信息 =====

    [ObservableProperty]
    private long _totalSelectedSize;

    [ObservableProperty]
    private string _totalSelectedSizeText = "0 B";

    [ObservableProperty]
    private long _totalCleanedSize;

    [ObservableProperty]
    private string _totalCleanedSizeText = "0 B";

    // ===== 状态 =====

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isCleaning;

    [ObservableProperty]
    private string _statusMessage = @"点击""扫描""查看可清理项目";

    [ObservableProperty]
    private double _cleanProgress;

    [ObservableProperty]
    private string _currentCleaningItem = string.Empty;

    [ObservableProperty]
    private bool _showSafeOnly = true;

    public CleanupViewModel(ICleanupService cleanupService)
    {
        _cleanupService = cleanupService;
        LoadCleanupItems();
    }

    /// <summary>
    /// 初始化清理项目列表
    /// </summary>
    private void LoadCleanupItems()
    {
        foreach (var existingItem in CleanupItems)
            existingItem.PropertyChanged -= CleanupItem_PropertyChanged;

        var items = _cleanupService.GetCleanupItems();
        CleanupItems.Clear();
        foreach (var item in items)
        {
            item.PropertyChanged += CleanupItem_PropertyChanged;
            CleanupItems.Add(item);
        }

        UpdateTotalSelectedSize();
    }

    private void CleanupItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CleanupItem.IsSelected) or nameof(CleanupItem.EstimatedSize))
            UpdateTotalSelectedSize();
    }

    /// <summary>
    /// 扫描各清理项的可释放大小
    /// </summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsScanning || IsCleaning) return;

        IsScanning = true;
        StatusMessage = "正在扫描可清理空间...";
        _cts = new CancellationTokenSource();

        try
        {
            await _cleanupService.ScanEstimatedSizesAsync(CleanupItems, _cts.Token);
            UpdateTotalSelectedSize();
            StatusMessage = $"扫描完成，共可释放 {TotalSelectedSizeText}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "扫描已取消";
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

    /// <summary>
    /// 执行清理操作
    /// </summary>
    [RelayCommand]
    private async Task CleanAsync()
    {
        if (IsCleaning || IsScanning) return;

        var selectedItems = CleanupItems.Where(i => i.IsSelected).ToList();
        if (selectedItems.Count == 0)
        {
            StatusMessage = "请先选择要清理的项目";
            return;
        }

        IsCleaning = true;
        CleanProgress = 0;
        StatusMessage = "正在清理...";
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<CleanupProgress>(p =>
            {
                CurrentCleaningItem = p.CurrentItemName;
                if (p.ItemsTotal > 0)
                    CleanProgress = (double)p.ItemsCompleted / p.ItemsTotal * 100.0;
                StatusMessage = $"正在清理：{p.CurrentItemName}";
            });

            long freed = await _cleanupService.CleanAsync(selectedItems, progress, _cts.Token);
            TotalCleanedSize = freed;
            TotalCleanedSizeText = FileSizeHelper.Format(freed);
            CleanProgress = 100;
            StatusMessage = $"清理完成！共释放 {TotalCleanedSizeText}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "清理已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"清理出错：{ex.Message}";
        }
        finally
        {
            IsCleaning = false;
        }
    }

    /// <summary>
    /// 取消正在进行的扫描或清理操作
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 全选安全（🟢）级别的清理项
    /// </summary>
    [RelayCommand]
    private void SelectSafeItems()
    {
        foreach (var item in CleanupItems)
            item.IsSelected = item.Safety == SafetyLevel.Safe;
        UpdateTotalSelectedSize();
    }

    /// <summary>
    /// 更新已选中项目的总大小统计
    /// </summary>
    private void UpdateTotalSelectedSize()
    {
        TotalSelectedSize = CleanupItems.Where(i => i.IsSelected).Sum(i => i.EstimatedSize);
        TotalSelectedSizeText = FileSizeHelper.Format(TotalSelectedSize);
    }
}

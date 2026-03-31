using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Helpers;
using DiskSlim.Models;
using DiskSlim.Services;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace DiskSlim.ViewModels;

/// <summary>
/// 智能清理 ViewModel，管理清理项目的扫描、选择和执行
/// </summary>
public partial class CleanupViewModel : ObservableObject
{
    private readonly ICleanupService _cleanupService;
    private readonly ICleanupReportService _reportService;
    private CancellationTokenSource? _cts;

    private enum AdminAction
    {
        Cancel,
        ContinueWithoutAdmin,
        RestartAsAdmin
    }

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
    private string _statusMessage = Localizer.Get("Vm.Cleanup.ClickScan", "点击\"扫描\"查看可清理项目");

    [ObservableProperty]
    private double _cleanProgress;

    [ObservableProperty]
    private string _currentCleaningItem = string.Empty;

    [ObservableProperty]
    private bool _showSafeOnly = true;

    // ===== UI 引用（用于弹出确认对话框）=====
    /// <summary>页面中的 XamlRoot，由 View 代码层设置</summary>
    public Microsoft.UI.Xaml.XamlRoot? XamlRoot { get; set; }

    public CleanupViewModel(ICleanupService cleanupService, ICleanupReportService reportService)
    {
        _cleanupService = cleanupService;
        _reportService = reportService;
        LoadCleanupItems();

        if (!AdminHelper.IsRunningAsAdmin())
            StatusMessage = Localizer.Get("Vm.Cleanup.NonAdminMode", "当前为普通用户模式（部分项目需管理员权限）");
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
        StatusMessage = Localizer.Get("Vm.Cleanup.Scanning", "正在扫描可清理空间...");
        _cts = new CancellationTokenSource();

        try
        {
            await _cleanupService.ScanEstimatedSizesAsync(CleanupItems, _cts.Token);
            UpdateTotalSelectedSize();
            StatusMessage = Localizer.Format("Vm.Cleanup.ScanCompleted", "扫描完成，共可释放 {0}", TotalSelectedSizeText);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localizer.Get("Vm.Cleanup.ScanCanceled", "扫描已取消");
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.Cleanup.ScanError", "扫描出错：{0}", ex.Message);
        }
        finally
        {
            IsScanning = false;
        }
    }

    /// <summary>
    /// 执行清理操作，针对🟡和🔴级别项目弹出确认对话框
    /// </summary>
    [RelayCommand]
    private async Task CleanAsync()
    {
        if (IsCleaning || IsScanning) return;

        var selectedItems = CleanupItems.Where(i => i.IsSelected).ToList();
        if (selectedItems.Count == 0)
        {
            StatusMessage = Localizer.Get("Vm.Cleanup.SelectItemsFirst", "请先选择要清理的项目");
            return;
        }

        var runnableItems = selectedItems.ToList();
        int skippedByPermission = 0;

        // 非管理员时：对需管理员项目进行按需提权处理
        if (!AdminHelper.IsRunningAsAdmin())
        {
            var adminItems = selectedItems.Where(i => i.RequiresAdmin).ToList();
            if (adminItems.Count > 0)
            {
                var action = XamlRoot != null
                    ? await ShowAdminRequirementDialogAsync(adminItems)
                    : AdminAction.ContinueWithoutAdmin;

                if (action == AdminAction.Cancel)
                {
                    StatusMessage = Localizer.Get("Vm.Cleanup.Canceled", "清理已取消");
                    return;
                }

                if (action == AdminAction.RestartAsAdmin)
                {
                    StatusMessage = Localizer.Get("Vm.Cleanup.RequestAdmin", "正在请求管理员权限...");
                    AdminHelper.RestartAsAdmin();
                    return;
                }

                runnableItems = selectedItems.Where(i => !i.RequiresAdmin).ToList();
                skippedByPermission = selectedItems.Count - runnableItems.Count;
            }
        }

        if (runnableItems.Count == 0)
        {
            StatusMessage = Localizer.Get("Vm.Cleanup.AllRequireAdmin", "当前所选项目均需要管理员权限，请提权后重试");
            return;
        }

        // 检查是否包含🟡或🔴级别项目，需要确认
        var cautionItems = runnableItems.Where(i => i.Safety == SafetyLevel.Caution).ToList();
        var dangerItems = runnableItems.Where(i => i.Safety == SafetyLevel.Danger).ToList();

        if ((cautionItems.Count > 0 || dangerItems.Count > 0) && XamlRoot != null)
        {
            bool confirmed = await ShowConfirmationDialogAsync(cautionItems, dangerItems);
            if (!confirmed)
            {
                StatusMessage = Localizer.Get("Vm.Cleanup.Canceled", "清理已取消");
                return;
            }
        }

        IsCleaning = true;
        CleanProgress = 0;
        StatusMessage = Localizer.Get("Vm.Cleanup.Cleaning", "正在清理...");
        _cts = new CancellationTokenSource();

        var report = new CleanupReport { StartedAt = DateTime.Now };

        try
        {
            IProgress<CleanupProgress> progress = new Progress<CleanupProgress>(p =>
            {
                CurrentCleaningItem = p.CurrentItemName;
                if (p.ItemsTotal > 0)
                    CleanProgress = (double)p.ItemsCompleted / p.ItemsTotal * 100.0;
                StatusMessage = Localizer.Format("Vm.Cleanup.CleaningItem", "正在清理：{0}", p.CurrentItemName);
            });

            // 逐项清理并记录明细
            long totalFreed = 0;
            int completed = 0;
            foreach (var item in runnableItems)
            {
                if (_cts.Token.IsCancellationRequested) break;
                if (item.CleanAction == null) continue;

                var reportItem = new CleanupReportItem { Name = item.Name };
                try
                {
                    var itemProgress = new Progress<long>(bytes =>
                    {
                        progress.Report(new CleanupProgress(item.Name, totalFreed + bytes, completed, runnableItems.Count));
                    });
                    long freed = await item.CleanAction(itemProgress, _cts.Token);
                    reportItem.FreedBytes = freed;
                    reportItem.Success = true;
                    totalFreed += freed;
                    item.IsCleaned = true;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    reportItem.Success = false;
                    reportItem.ErrorMessage = ex.Message;
                }
                report.Items.Add(reportItem);
                completed++;
            }

            TotalCleanedSize = totalFreed;
            TotalCleanedSizeText = FileSizeHelper.Format(totalFreed);
            CleanProgress = 100;
            StatusMessage = skippedByPermission > 0
                ? Localizer.Format("Vm.Cleanup.CleanCompletedWithSkipped", "清理完成！共释放 {0}，另有 {1} 项因权限不足未执行", TotalCleanedSizeText, skippedByPermission)
                : Localizer.Format("Vm.Cleanup.CleanCompleted", "清理完成！共释放 {0}", TotalCleanedSizeText);

            // 仅在至少清理了一项时保存报告
            if (report.Items.Count > 0)
            {
                report.CompletedAt = DateTime.Now;
                report.TotalFreedBytes = totalFreed;
                try { await _reportService.SaveReportAsync(report); }
                catch { }
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localizer.Get("Vm.Cleanup.Canceled", "清理已取消");
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.Cleanup.CleanError", "清理出错：{0}", ex.Message);
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
    /// 全选非危险（🟢安全 + 🟡谨慎）级别的清理项
    /// </summary>
    [RelayCommand]
    private void SelectNonDangerItems()
    {
        foreach (var item in CleanupItems)
            item.IsSelected = item.Safety != SafetyLevel.Danger;
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

    private async Task<AdminAction> ShowAdminRequirementDialogAsync(List<CleanupItem> adminItems)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Localizer.Get("Vm.Cleanup.AdminDialog.ItemsNeedAdmin", "以下项目需要管理员权限："));
        sb.AppendLine();
        foreach (var item in adminItems)
            sb.AppendLine($"  • {item.Name}");
        sb.AppendLine();
        sb.AppendLine(Localizer.Get("Vm.Cleanup.AdminDialog.ChooseAction", "你可以选择仅清理当前可执行项目，或提权后重启应用继续。 "));

        var dialog = new ContentDialog
        {
            Title = Localizer.Get("Vm.Cleanup.AdminDialog.Title", "需要管理员权限"),
            Content = sb.ToString().Trim(),
            PrimaryButtonText = Localizer.Get("Vm.Cleanup.AdminDialog.Primary", "仅清理可执行项"),
            SecondaryButtonText = Localizer.Get("Vm.Cleanup.AdminDialog.Secondary", "提权后重启"),
            CloseButtonText = Localizer.Get("Vm.Cleanup.AdminDialog.Close", "取消"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result switch
        {
            ContentDialogResult.Primary => AdminAction.ContinueWithoutAdmin,
            ContentDialogResult.Secondary => AdminAction.RestartAsAdmin,
            _ => AdminAction.Cancel
        };
    }

    /// <summary>
    /// 弹出确认对话框，提示用户所选的🟡/🔴级别风险
    /// </summary>
    private async Task<bool> ShowConfirmationDialogAsync(
        List<CleanupItem> cautionItems,
        List<CleanupItem> dangerItems)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Localizer.Get("Vm.Cleanup.ConfirmDialog.Intro", "您选中了以下需要注意的清理项目，请确认："));
        sb.AppendLine();

        if (dangerItems.Count > 0)
        {
            sb.AppendLine(Localizer.Get("Vm.Cleanup.ConfirmDialog.Danger", "🔴 危险项目（操作不可逆，请慎重）："));
            foreach (var item in dangerItems)
                sb.AppendLine($"  • {item.Name}");
            sb.AppendLine();
        }

        if (cautionItems.Count > 0)
        {
            sb.AppendLine(Localizer.Get("Vm.Cleanup.ConfirmDialog.Caution", "🟡 谨慎项目（建议确认后清理）："));
            foreach (var item in cautionItems)
                sb.AppendLine($"  • {item.Name}");
        }

        var dialog = new ContentDialog
        {
            Title = Localizer.Get("Vm.Cleanup.ConfirmDialog.Title", "确认清理"),
            Content = sb.ToString().Trim(),
            PrimaryButtonText = Localizer.Get("Vm.Cleanup.ConfirmDialog.Primary", "确认清理"),
            CloseButtonText = Localizer.Get("Vm.Cleanup.ConfirmDialog.Close", "取消"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}

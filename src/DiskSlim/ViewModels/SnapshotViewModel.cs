using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Models;
using DiskSlim.Services;
using System.Collections.ObjectModel;

namespace DiskSlim.ViewModels;

/// <summary>
/// 历史快照页面 ViewModel，管理磁盘扫描快照的保存、查看和对比
/// </summary>
public partial class SnapshotViewModel : ObservableObject
{
    private readonly ISnapshotService _snapshotService;
    private readonly IDiskScanService _diskScanService;

    /// <summary>快照列表</summary>
    public ObservableCollection<DiskSnapshot> Snapshots { get; } = new();

    /// <summary>对比结果列表</summary>
    public ObservableCollection<SnapshotDiffItem> DiffItems { get; } = new();

    [ObservableProperty]
    private DiskSnapshot? _selectedSnapshot;

    [ObservableProperty]
    private DiskSnapshot? _compareSnapshot1;

    [ObservableProperty]
    private DiskSnapshot? _compareSnapshot2;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isCreating;

    [ObservableProperty]
    private bool _isComparing;

    [ObservableProperty]
    private string _statusMessage = "加载快照中...";

    [ObservableProperty]
    private string _newSnapshotLabel = string.Empty;

    [ObservableProperty]
    private bool _hasSnapshots;

    [ObservableProperty]
    private bool _hasDiffResults;

    [ObservableProperty]
    private string _diffSummary = string.Empty;

    [ObservableProperty]
    private long _diskDeltaBytes;

    [ObservableProperty]
    private string _diskDeltaText = string.Empty;

    private CancellationTokenSource? _cts;

    public SnapshotViewModel(ISnapshotService snapshotService, IDiskScanService diskScanService)
    {
        _snapshotService = snapshotService;
        _diskScanService = diskScanService;
    }

    /// <summary>
    /// 加载所有快照列表
    /// </summary>
    [RelayCommand]
    public async Task LoadSnapshotsAsync()
    {
        IsLoading = true;
        try
        {
            var snapshots = await _snapshotService.GetSnapshotsAsync();
            Snapshots.Clear();
            foreach (var s in snapshots)
                Snapshots.Add(s);

            HasSnapshots = Snapshots.Count > 0;
            StatusMessage = HasSnapshots ? $"共 {Snapshots.Count} 个快照" : "暂无快照，点击【新建快照】保存当前状态";

            if (HasSnapshots && SelectedSnapshot == null)
                SelectedSnapshot = Snapshots[0];
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 创建新快照（扫描C盘并保存）
    /// </summary>
    [RelayCommand]
    public async Task CreateSnapshotAsync()
    {
        IsCreating = true;
        StatusMessage = "正在创建快照...";
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            var snapshot = await _snapshotService.CreateSnapshotAsync(
                NewSnapshotLabel, progress, _cts.Token);

            Snapshots.Insert(0, snapshot);
            HasSnapshots = true;
            SelectedSnapshot = snapshot;
            NewSnapshotLabel = string.Empty;
            StatusMessage = $"快照已保存：{snapshot.SnapshotTimeText}（已用 {snapshot.UsedBytesText}）";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "快照创建已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"创建快照失败：{ex.Message}";
        }
        finally
        {
            IsCreating = false;
            _cts = null;
        }
    }

    /// <summary>
    /// 删除选中的快照
    /// </summary>
    [RelayCommand]
    private async Task DeleteSnapshotAsync()
    {
        if (SelectedSnapshot == null) return;

        try
        {
            int id = SelectedSnapshot.Id;
            await _snapshotService.DeleteSnapshotAsync(id);
            Snapshots.Remove(SelectedSnapshot);
            HasSnapshots = Snapshots.Count > 0;
            SelectedSnapshot = HasSnapshots ? Snapshots[0] : null;
            StatusMessage = $"快照已删除，共 {Snapshots.Count} 个快照";
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 对比两个快照
    /// </summary>
    [RelayCommand]
    public async Task CompareSnapshotsAsync()
    {
        if (CompareSnapshot1 == null || CompareSnapshot2 == null)
        {
            StatusMessage = "请先选择两个快照进行对比";
            return;
        }

        if (CompareSnapshot1.Id == CompareSnapshot2.Id)
        {
            StatusMessage = "请选择不同的两个快照";
            return;
        }

        IsComparing = true;
        StatusMessage = "正在对比快照...";

        try
        {
            // 确保旧快照在前
            var (oldSnap, newSnap) = CompareSnapshot1.SnapshotTime < CompareSnapshot2.SnapshotTime
                ? (CompareSnapshot1, CompareSnapshot2)
                : (CompareSnapshot2, CompareSnapshot1);

            var diffs = await _snapshotService.CompareSnapshotsAsync(oldSnap.Id, newSnap.Id);

            DiffItems.Clear();
            foreach (var item in diffs)
                DiffItems.Add(item);

            HasDiffResults = DiffItems.Count > 0;

            // 计算总体空间变化
            DiskDeltaBytes = newSnap.UsedBytes - oldSnap.UsedBytes;
            string deltaPrefix = DiskDeltaBytes >= 0 ? "+" : "-";
            DiskDeltaText = $"{deltaPrefix}{Helpers.FileSizeHelper.Format(Math.Abs(DiskDeltaBytes))}";

            StatusMessage = $"对比完成：{oldSnap.SnapshotTimeText} → {newSnap.SnapshotTimeText}，" +
                            $"磁盘变化 {DiskDeltaText}";
            DiffSummary = $"新快照磁盘已用：{newSnap.UsedBytesText}，旧快照：{oldSnap.UsedBytesText}，变化：{DiskDeltaText}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"对比失败：{ex.Message}";
        }
        finally
        {
            IsComparing = false;
        }
    }

    /// <summary>
    /// 取消快照创建
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }
}

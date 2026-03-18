using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Models;
using DiskSlim.Services;
using System.Collections.ObjectModel;

namespace DiskSlim.ViewModels;

/// <summary>
/// 旧文件/临时文件检测页面 ViewModel
/// </summary>
public partial class OldFilesViewModel : ObservableObject
{
    private readonly IOldFilesService _oldFilesService;

    /// <summary>检测到的文件列表</summary>
    public ObservableCollection<OldFileItem> FileItems { get; } = new();

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isDeleting;

    [ObservableProperty]
    private string _statusMessage = "点击扫描按钮开始检测旧文件或临时文件";

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private int _selectedNotAccessedDays = 90;

    [ObservableProperty]
    private string _selectedScanType = "旧文件";

    [ObservableProperty]
    private long _totalSelectedSize;

    [ObservableProperty]
    private string _totalSelectedSizeText = "0 B";

    [ObservableProperty]
    private int _selectedCount;

    /// <summary>可配置的未访问天数选项</summary>
    public List<int> NotAccessedDaysOptions { get; } = [30, 90, 180, 365];

    /// <summary>扫描类型选项</summary>
    public List<string> ScanTypeOptions { get; } = ["旧文件", "临时文件", "空文件和转储"];

    /// <summary>根路径（默认用户目录）</summary>
    private string RootPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private CancellationTokenSource? _cts;

    public OldFilesViewModel(IOldFilesService oldFilesService)
    {
        _oldFilesService = oldFilesService;
    }

    /// <summary>
    /// 开始扫描
    /// </summary>
    [RelayCommand]
    public async Task ScanAsync()
    {
        IsScanning = true;
        FileItems.Clear();
        HasResults = false;
        StatusMessage = "扫描中，请稍候...";
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = $"扫描中：{msg}");
            IReadOnlyList<OldFileItem> results;

            switch (SelectedScanType)
            {
                case "旧文件":
                    results = await _oldFilesService.ScanOldFilesAsync(
                        RootPath, SelectedNotAccessedDays, progress, _cts.Token);
                    break;
                case "临时文件":
                    results = await _oldFilesService.ScanTempFilesAsync(
                        RootPath, progress, _cts.Token);
                    break;
                default:
                    results = await _oldFilesService.ScanSpecialFilesAsync(
                        RootPath, progress, _cts.Token);
                    break;
            }

            foreach (var item in results)
                FileItems.Add(item);

            HasResults = FileItems.Count > 0;
            StatusMessage = HasResults
                ? $"扫描完成，发现 {FileItems.Count} 个文件"
                : "未发现符合条件的文件";

            UpdateSelectionStats();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "扫描已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"扫描失败：{ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cts = null;
        }
    }

    /// <summary>
    /// 取消扫描
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 全选
    /// </summary>
    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in FileItems)
            item.IsSelected = true;
        UpdateSelectionStats();
    }

    /// <summary>
    /// 取消全选
    /// </summary>
    [RelayCommand]
    private void SelectNone()
    {
        foreach (var item in FileItems)
            item.IsSelected = false;
        UpdateSelectionStats();
    }

    /// <summary>
    /// 删除选中文件到回收站
    /// </summary>
    [RelayCommand]
    public async Task DeleteSelectedAsync()
    {
        var toDelete = FileItems.Where(f => f.IsSelected).Select(f => f.FullPath).ToList();
        if (toDelete.Count == 0)
        {
            StatusMessage = "请先勾选要删除的文件";
            return;
        }

        IsDeleting = true;
        StatusMessage = "正在移动到回收站...";

        try
        {
            var progress = new Progress<string>(name => StatusMessage = $"删除：{name}");
            int count = await _oldFilesService.BatchDeleteToRecycleBinAsync(toDelete, progress);

            // 从列表中移除已删除的文件
            var deleted = FileItems.Where(f => f.IsSelected).ToList();
            foreach (var item in deleted)
                FileItems.Remove(item);

            HasResults = FileItems.Count > 0;
            StatusMessage = $"已将 {count} 个文件移动到回收站";
            UpdateSelectionStats();
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败：{ex.Message}";
        }
        finally
        {
            IsDeleting = false;
        }
    }

    /// <summary>
    /// 在资源管理器中打开文件位置
    /// </summary>
    [RelayCommand]
    private void OpenInExplorer(OldFileItem? item)
    {
        if (item == null) return;
        _oldFilesService.OpenInExplorer(item.FullPath);
    }

    /// <summary>
    /// 切换文件选中状态并更新统计
    /// </summary>
    [RelayCommand]
    public void ToggleSelection(OldFileItem? item)
    {
        if (item == null) return;
        item.IsSelected = !item.IsSelected;
        UpdateSelectionStats();
    }

    /// <summary>
    /// 更新选中统计信息
    /// </summary>
    private void UpdateSelectionStats()
    {
        var selected = FileItems.Where(f => f.IsSelected).ToList();
        SelectedCount = selected.Count;
        TotalSelectedSize = selected.Sum(f => f.SizeBytes);
        TotalSelectedSizeText = Helpers.FileSizeHelper.Format(TotalSelectedSize);
    }
}

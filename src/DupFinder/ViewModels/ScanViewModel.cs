using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DupFinder.Helpers;
using DupFinder.Models;
using DupFinder.Services;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;

namespace DupFinder.ViewModels;

/// <summary>
/// 重复文件扫描页面 ViewModel
/// </summary>
public partial class ScanViewModel : ObservableObject
{
    private readonly IDuplicateScanService _scanService;
    private CancellationTokenSource? _cts;

    // ─── 扫描结果 ────────────────────────────────────────────────────

    /// <summary>扫描到的重复文件组列表</summary>
    public ObservableCollection<DuplicateGroup> DuplicateGroups { get; } = new();

    // ─── 扫描配置 ────────────────────────────────────────────────────

    /// <summary>要扫描的文件夹路径（多路径用分号分隔）</summary>
    [ObservableProperty]
    private string _scanFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>是否递归扫描子目录</summary>
    [ObservableProperty]
    private bool _includeSubdirectories = true;

    /// <summary>最小文件大小选项显示文本</summary>
    [ObservableProperty]
    private string _selectedMinSize = "1 KB";

    /// <summary>文件类型过滤</summary>
    [ObservableProperty]
    private string _selectedFileType = Localizer.Get("Vm.Scan.AllFiles", "所有文件");

    // ─── 状态标志 ────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isDeleting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    private int _totalGroupCount;

    [ObservableProperty]
    private string _statusMessage = Localizer.Get("Vm.Scan.Hint", "选择文件夹后点击「开始扫描」查找重复文件");

    [ObservableProperty]
    private double _scanProgressValue;

    [ObservableProperty]
    private bool _isProgressIndeterminate = true;

    // ─── 选中统计 ────────────────────────────────────────────────────

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private string _totalSelectedSizeText = "0 B";

    [ObservableProperty]
    private string _totalWasteBytesText = "0 B";

    // ─── 配置选项列表 ────────────────────────────────────────────────

    /// <summary>文件类型过滤选项</summary>
    public List<string> FileTypeOptions { get; } =
    [
        Localizer.Get("Vm.Scan.AllFiles", "所有文件"),
        "图片", "视频", "音乐", "文档", "压缩包", "程序"
    ];

    /// <summary>最小文件大小选项</summary>
    public List<string> MinSizeOptions { get; } =
        ["1 KB", "10 KB", "100 KB", "1 MB", "10 MB", "100 MB"];

    public bool HasResults => TotalGroupCount > 0;

    public ScanViewModel(IDuplicateScanService scanService)
    {
        _scanService = scanService;
    }

    // ─── 命令 ────────────────────────────────────────────────────────

    /// <summary>
    /// 打开文件夹选择对话框
    /// </summary>
    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
        picker.FileTypeFilter.Add("*");

        // 需要绑定到窗口句柄
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder != null)
            ScanFolder = folder.Path;
    }

    /// <summary>
    /// 开始扫描
    /// </summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanFolder))
        {
            StatusMessage = Localizer.Get("Vm.Scan.NoFolder", "请先选择要扫描的文件夹");
            return;
        }

        IsScanning = true;
        IsProgressIndeterminate = true;
        ScanProgressValue = 0;
        DuplicateGroups.Clear();
        TotalGroupCount = 0;
        TotalWasteBytesText = "0 B";
        StatusMessage = Localizer.Get("Vm.Scan.Starting", "正在准备扫描…");
        _cts = new CancellationTokenSource();

        try
        {
            var options = BuildScanOptions();
            var progress = new Progress<ScanProgress>(p =>
            {
                if (p.TotalCount > 0)
                {
                    IsProgressIndeterminate = false;
                    ScanProgressValue = (double)p.ScannedCount / p.TotalCount * 100.0;
                }
                StatusMessage = string.IsNullOrEmpty(p.CurrentFile)
                    ? p.Phase
                    : Localizer.Format("Vm.Scan.ScanningFile", "{0}  {1}", p.Phase, p.CurrentFile);
            });

            var groups = await _scanService.ScanAsync(options, progress, _cts.Token);

            foreach (var g in groups)
                DuplicateGroups.Add(g);

            TotalGroupCount = DuplicateGroups.Count;
            long totalWaste = DuplicateGroups.Sum(g => g.WasteBytes);
            TotalWasteBytesText = FileSizeHelper.Format(totalWaste);

            StatusMessage = TotalGroupCount > 0
                ? Localizer.Format("Vm.Scan.Done",
                    "扫描完成，发现 {0} 组重复文件，可释放 {1}",
                    TotalGroupCount, TotalWasteBytesText)
                : Localizer.Get("Vm.Scan.NoDup", "扫描完成，未发现重复文件");

            UpdateSelectionStats();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localizer.Get("Vm.Scan.Canceled", "扫描已取消");
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.Scan.Failed", "扫描失败：{0}", ex.Message);
        }
        finally
        {
            IsScanning = false;
            IsProgressIndeterminate = false;
            ScanProgressValue = 100;
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
    /// 全选所有副本（每组保留最新文件，其余选中）
    /// </summary>
    [RelayCommand]
    private void SelectAllDuplicates()
    {
        foreach (var group in DuplicateGroups)
        {
            foreach (var file in group.Files)
                file.IsSelected = !file.IsKeeper;
        }
        UpdateSelectionStats();
    }

    /// <summary>
    /// 取消全选
    /// </summary>
    [RelayCommand]
    private void SelectNone()
    {
        foreach (var group in DuplicateGroups)
        {
            foreach (var file in group.Files)
                file.IsSelected = false;
        }
        UpdateSelectionStats();
    }

    /// <summary>
    /// 每组保留最新文件，选中其余副本
    /// </summary>
    [RelayCommand]
    private void KeepNewest()
    {
        foreach (var group in DuplicateGroups)
        {
            // 清除所有 keeper 标记
            foreach (var f in group.Files)
                f.IsKeeper = false;

            // 最后修改时间最新的文件为 keeper
            var newest = group.Files.OrderByDescending(f => f.LastModified).First();
            newest.IsKeeper = true;

            foreach (var f in group.Files)
                f.IsSelected = !f.IsKeeper;
        }
        UpdateSelectionStats();
    }

    /// <summary>
    /// 每组保留最旧文件，选中其余副本
    /// </summary>
    [RelayCommand]
    private void KeepOldest()
    {
        foreach (var group in DuplicateGroups)
        {
            foreach (var f in group.Files)
                f.IsKeeper = false;

            var oldest = group.Files.OrderBy(f => f.LastModified).First();
            oldest.IsKeeper = true;

            foreach (var f in group.Files)
                f.IsSelected = !f.IsKeeper;
        }
        UpdateSelectionStats();
    }

    /// <summary>
    /// 删除选中文件到回收站
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var toDelete = DuplicateGroups
            .SelectMany(g => g.Files)
            .Where(f => f.IsSelected)
            .Select(f => f.FullPath)
            .ToList();

        if (toDelete.Count == 0)
        {
            StatusMessage = Localizer.Get("Vm.Scan.NoneSelected", "请先勾选要删除的文件");
            return;
        }

        IsDeleting = true;
        StatusMessage = Localizer.Get("Vm.Scan.Deleting", "正在移动到回收站…");

        try
        {
            var progress = new Progress<string>(name =>
                StatusMessage = Localizer.Format("Vm.Scan.DeletingItem", "删除：{0}", name));

            int deleted = await _scanService.BatchDeleteToRecycleBinAsync(toDelete, progress);

            // 从列表中移除已删除文件，并清理空的重复组
            var toDeleteSet = new HashSet<string>(toDelete, StringComparer.OrdinalIgnoreCase);
            var emptyGroups = new List<DuplicateGroup>();

            foreach (var group in DuplicateGroups)
            {
                var removedFiles = group.Files.Where(f => toDeleteSet.Contains(f.FullPath)).ToList();
                foreach (var f in removedFiles)
                    group.Files.Remove(f);

                if (group.Files.Count < 2)
                    emptyGroups.Add(group);
            }

            foreach (var g in emptyGroups)
                DuplicateGroups.Remove(g);

            TotalGroupCount = DuplicateGroups.Count;
            long totalWaste = DuplicateGroups.Sum(g => g.WasteBytes);
            TotalWasteBytesText = FileSizeHelper.Format(totalWaste);

            StatusMessage = Localizer.Format("Vm.Scan.DeleteDone", "已将 {0} 个文件移动到回收站", deleted);
            UpdateSelectionStats();
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.Scan.DeleteFailed", "删除失败：{0}", ex.Message);
        }
        finally
        {
            IsDeleting = false;
        }
    }

    /// <summary>
    /// 在资源管理器中打开文件
    /// </summary>
    [RelayCommand]
    private void OpenInExplorer(DuplicateFileItem? item)
    {
        if (item == null) return;
        _scanService.OpenInExplorer(item.FullPath);
    }

    /// <summary>
    /// 切换文件选中状态并更新统计
    /// </summary>
    [RelayCommand]
    private void ToggleSelection(DuplicateFileItem? item)
    {
        if (item == null) return;
        item.IsSelected = !item.IsSelected;
        UpdateSelectionStats();
    }

    // ─── 私有方法 ────────────────────────────────────────────────────

    /// <summary>
    /// 根据当前 UI 配置构建扫描选项
    /// </summary>
    private ScanOptions BuildScanOptions()
    {
        long minBytes = SelectedMinSize switch
        {
            "1 KB" => 1_024,
            "10 KB" => 10_240,
            "100 KB" => 102_400,
            "1 MB" => 1_048_576,
            "10 MB" => 10_485_760,
            "100 MB" => 104_857_600,
            _ => 1_024
        };

        string fileTypeFilter = SelectedFileType == Localizer.Get("Vm.Scan.AllFiles", "所有文件")
            ? string.Empty
            : SelectedFileType;

        // 支持多路径（分号分隔）
        var folders = ScanFolder
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(Directory.Exists)
            .ToList();

        if (folders.Count == 0)
            folders.Add(ScanFolder.Trim());

        return new ScanOptions
        {
            ScanFolders = folders,
            IncludeSubdirectories = IncludeSubdirectories,
            MinFileSizeBytes = minBytes,
            FileTypeFilter = fileTypeFilter,
            SkipHiddenFiles = true,
            SkipSystemFiles = true
        };
    }

    /// <summary>
    /// 更新已选中文件的统计信息
    /// </summary>
    private void UpdateSelectionStats()
    {
        var selectedFiles = DuplicateGroups
            .SelectMany(g => g.Files)
            .Where(f => f.IsSelected)
            .ToList();

        SelectedCount = selectedFiles.Count;
        long totalBytes = selectedFiles.Sum(f => f.SizeBytes);
        TotalSelectedSizeText = FileSizeHelper.Format(totalBytes);
    }
}

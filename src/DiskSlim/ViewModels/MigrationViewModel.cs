using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Helpers;
using DiskSlim.Models;
using DiskSlim.Services;
using System.Collections.ObjectModel;

namespace DiskSlim.ViewModels;

/// <summary>
/// 文件夹迁移 ViewModel，管理用户文件夹从C盘迁移到其他盘的流程
/// </summary>
public partial class MigrationViewModel : ObservableObject
{
    private readonly IMigrationService _migrationService;
    private CancellationTokenSource? _cts;

    // ===== 可迁移文件夹列表 =====
    public ObservableCollection<UserFolderInfo> MigratableFolders { get; } = new();

    // ===== 迁移历史 =====
    public ObservableCollection<MigrationTask> MigrationHistory { get; } = new();

    // ===== 可选目标盘符列表（扫描系统中实际存在的非系统盘）=====
    public ObservableCollection<string> AvailableDrives { get; } = new();

    // ===== 当前选择 =====

    [ObservableProperty]
    private UserFolderInfo? _selectedFolder;

    [ObservableProperty]
    private string _destinationDrive = string.Empty;

    [ObservableProperty]
    private string _destinationPath = string.Empty;

    // ===== 状态 =====

    [ObservableProperty]
    private bool _isMigrating;

    [ObservableProperty]
    private bool _isLoadingFolders;

    [ObservableProperty]
    private double _migrationProgress;

    [ObservableProperty]
    private string _statusMessage = "选择要迁移的文件夹和目标盘";

    [ObservableProperty]
    private string _currentFile = string.Empty;

    [ObservableProperty]
    private bool _hasSpaceError;

    [ObservableProperty]
    private string _spaceErrorMessage = string.Empty;

    public MigrationViewModel(IMigrationService migrationService)
    {
        _migrationService = migrationService;
        LoadDrives();
        LoadFolders();
    }

    /// <summary>
    /// 扫描系统中可用的非系统盘盘符
    /// </summary>
    private void LoadDrives()
    {
        AvailableDrives.Clear();
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                // 只显示固定盘，且不是系统盘（C:）
                if (drive.DriveType == DriveType.Fixed &&
                    !drive.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                {
                    AvailableDrives.Add(drive.Name.TrimEnd('\\'));
                }
            }
        }
        catch { }

        // 如有可用盘，默认选第一个；否则预填 D:
        DestinationDrive = AvailableDrives.Count > 0 ? AvailableDrives[0] : "D:";
    }

    /// <summary>
    /// 加载可迁移文件夹列表（并异步扫描各文件夹大小）
    /// </summary>
    private void LoadFolders()
    {
        var folders = _migrationService.GetMigratableFolders();
        MigratableFolders.Clear();
        foreach (var folder in folders)
            MigratableFolders.Add(folder);

        // 异步扫描文件夹大小（不阻塞 UI）
        _ = ScanFolderSizesAsync();
    }

    /// <summary>
    /// 在后台异步扫描各文件夹大小
    /// </summary>
    private async Task ScanFolderSizesAsync()
    {
        IsLoadingFolders = true;
        try
        {
            // 并行扫描以加快速度
            var tasks = MigratableFolders.Select(async folder =>
            {
                try
                {
                    folder.SizeBytes = await GetFolderSizeAsync(folder.CurrentPath);
                }
                catch { folder.SizeBytes = 0; }
                // 触发 UI 刷新（手动通知，因 UserFolderInfo 不是 ObservableObject）
                OnPropertyChanged(nameof(MigratableFolders));
            });
            await Task.WhenAll(tasks);
        }
        finally
        {
            IsLoadingFolders = false;
        }
    }

    /// <summary>
    /// 递归统计文件夹大小
    /// </summary>
    private static Task<long> GetFolderSizeAsync(string path)
    {
        return Task.Run(() =>
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { size += new FileInfo(file).Length; } catch { }
                }
            }
            catch { }
            return size;
        });
    }

    /// <summary>
    /// 验证目标路径空间是否足够
    /// </summary>
    [RelayCommand]
    private async Task ValidateSpaceAsync()
    {
        if (SelectedFolder == null) return;

        HasSpaceError = false;

        if (string.IsNullOrEmpty(DestinationDrive))
        {
            HasSpaceError = true;
            SpaceErrorMessage = "请先选择目标盘符";
            return;
        }

        // 检查目标盘是否存在
        if (!DriveInfo.GetDrives().Any(d => d.Name.StartsWith(DestinationDrive, StringComparison.OrdinalIgnoreCase)))
        {
            HasSpaceError = true;
            SpaceErrorMessage = $"目标盘 {DestinationDrive} 不存在，请选择其他盘符";
            return;
        }

        bool hasSpace = await _migrationService.ValidateDestinationSpaceAsync(
            SelectedFolder.CurrentPath, DestinationDrive);

        if (!hasSpace)
        {
            HasSpaceError = true;
            SpaceErrorMessage = $"目标盘 {DestinationDrive} 空间不足，请选择其他盘符";
        }
    }

    /// <summary>
    /// 执行迁移操作
    /// </summary>
    [RelayCommand]
    private async Task MigrateAsync()
    {
        if (SelectedFolder == null || IsMigrating) return;

        if (string.IsNullOrEmpty(DestinationDrive))
        {
            StatusMessage = "请先选择目标盘符";
            return;
        }

        if (string.IsNullOrEmpty(DestinationPath))
        {
            DestinationPath = Path.Combine(DestinationDrive + "\\",
                Path.GetFileName(SelectedFolder.CurrentPath));
        }

        // 迁移前验证空间
        await ValidateSpaceAsync();
        if (HasSpaceError) return;

        IsMigrating = true;
        MigrationProgress = 0;
        _cts = new CancellationTokenSource();

        var task = new MigrationTask
        {
            Name = $"迁移{SelectedFolder.DisplayName}",
            SourcePath = SelectedFolder.CurrentPath,
            DestinationPath = DestinationPath
        };

        try
        {
            var progress = new Progress<MigrationProgress>(p =>
            {
                StatusMessage = p.Stage;
                CurrentFile = p.CurrentFile;
                if (p.TotalBytes > 0)
                    MigrationProgress = (double)p.BytesCopied / p.TotalBytes * 100.0;
            });

            await _migrationService.ExecuteMigrationAsync(task, progress, _cts.Token);
            MigrationProgress = 100;
            StatusMessage = $"迁移完成！{SelectedFolder.DisplayName} 已迁移到 {DestinationPath}";
            MigrationHistory.Insert(0, task);
            DestinationPath = string.Empty; // 清空路径，供下次使用
            LoadFolders(); // 刷新状态
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "迁移已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"迁移失败：{ex.Message}";
        }
        finally
        {
            IsMigrating = false;
        }
    }

    /// <summary>
    /// 取消迁移
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 回退指定迁移任务
    /// </summary>
    [RelayCommand]
    private async Task RollbackAsync(MigrationTask task)
    {
        if (task == null || !task.CanRollback) return;

        try
        {
            await _migrationService.RollbackMigrationAsync(task);
            StatusMessage = $"已回退迁移：{task.Name}";
            LoadFolders();
        }
        catch (Exception ex)
        {
            StatusMessage = $"回退失败：{ex.Message}";
        }
    }
}

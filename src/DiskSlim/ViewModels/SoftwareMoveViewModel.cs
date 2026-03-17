using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Helpers;
using DiskSlim.Models;
using DiskSlim.Services;
using System.Collections.ObjectModel;

namespace DiskSlim.ViewModels;

/// <summary>
/// 软件搬家 ViewModel，管理将软件从C盘迁移到其他盘（通过符号链接）
/// </summary>
public partial class SoftwareMoveViewModel : ObservableObject
{
    private readonly ISoftwareScanService _softwareScanService;
    private readonly ISymlinkService _symlinkService;

    // ===== 软件列表 =====
    public ObservableCollection<SoftwareInfo> SoftwareList { get; } = new();

    // ===== 筛选选项 =====

    [ObservableProperty]
    private bool _showSystemDriveOnly = true;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    // ===== 当前选中软件 =====

    [ObservableProperty]
    private SoftwareInfo? _selectedSoftware;

    [ObservableProperty]
    private string _targetDrive = "D:";

    // ===== 状态 =====

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isMoving;

    [ObservableProperty]
    private string _statusMessage = @"点击""扫描软件""加载已安装软件列表";

    [ObservableProperty]
    private int _systemDriveSoftwareCount;

    [ObservableProperty]
    private string _systemDriveTotalSizeText = "--";

    public SoftwareMoveViewModel(ISoftwareScanService softwareScanService, ISymlinkService symlinkService)
    {
        _softwareScanService = softwareScanService;
        _symlinkService = symlinkService;
    }

    /// <summary>
    /// 扫描已安装的软件列表
    /// </summary>
    [RelayCommand]
    private async Task ScanSoftwareAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        StatusMessage = "正在扫描已安装软件...";

        try
        {
            var allSoftware = await _softwareScanService.ScanInstalledSoftwareAsync();
            var filtered = ShowSystemDriveOnly
                ? _softwareScanService.FilterSystemDriveSoftware(allSoftware)
                : allSoftware;

            SoftwareList.Clear();
            foreach (var sw in filtered)
                SoftwareList.Add(sw);

            SystemDriveSoftwareCount = _softwareScanService.FilterSystemDriveSoftware(allSoftware).Count;
            long totalSize = allSoftware
                .Where(s => s.IsOnSystemDrive && s.InstallSizeBytes > 0)
                .Sum(s => s.InstallSizeBytes);
            SystemDriveTotalSizeText = FileSizeHelper.Format(totalSize);

            StatusMessage = $"共找到 {SoftwareList.Count} 个软件（{SystemDriveTotalSizeText} 在C盘）";
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
    /// 将选中的软件从C盘迁移到目标盘（通过 Junction 符号链接）
    /// </summary>
    [RelayCommand]
    private async Task MoveSoftwareAsync()
    {
        if (SelectedSoftware == null || IsMoving) return;
        if (!SelectedSoftware.CanMigrate)
        {
            StatusMessage = "该软件无法迁移（安装路径不明确或已迁移）";
            return;
        }

        IsMoving = true;
        StatusMessage = $"正在迁移：{SelectedSoftware.DisplayName}...";

        try
        {
            string sourcePath = SelectedSoftware.InstallLocation.TrimEnd('\\');
            string folderName = Path.GetFileName(sourcePath);
            string targetPath = Path.Combine(TargetDrive, "Programs", folderName);

            // 确保目标目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            // 复制软件文件到目标路径
            await CopySoftwareFilesAsync(sourcePath, targetPath);

            // 先验证能否创建 Junction 链接（用临时路径测试），避免删除源目录后链接创建失败
            string tempJunction = sourcePath + "__slim_test__";
            bool testSuccess = await _symlinkService.CreateJunctionAsync(tempJunction, targetPath);
            if (testSuccess)
            {
                await _symlinkService.DeleteJunctionAsync(tempJunction);
            }
            else
            {
                // 无法创建链接，清理已复制的目标文件，报告错误
                try { Directory.Delete(targetPath, recursive: true); } catch { }
                StatusMessage = "创建符号链接失败，请以管理员权限运行";
                return;
            }

            // 链接可创建，删除源目录，建立正式 Junction
            Directory.Delete(sourcePath, recursive: true);
            await _symlinkService.CreateJunctionAsync(sourcePath, targetPath);

            SelectedSoftware.MigratedToPath = targetPath;
            SelectedSoftware.CanMigrate = false;
            StatusMessage = $"{SelectedSoftware.DisplayName} 已迁移到 {targetPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"迁移失败：{ex.Message}";
        }
        finally
        {
            IsMoving = false;
        }
    }

    /// <summary>
    /// 复制软件文件到目标路径（使用队列避免深层递归）
    /// </summary>
    private static async Task CopySoftwareFilesAsync(string source, string destination)
    {
        await Task.Run(() =>
        {
            var queue = new Queue<(string src, string dst)>();
            queue.Enqueue((source, destination));

            while (queue.Count > 0)
            {
                var (src, dst) = queue.Dequeue();
                Directory.CreateDirectory(dst);

                foreach (var file in new DirectoryInfo(src).GetFiles())
                {
                    try
                    {
                        file.CopyTo(Path.Combine(dst, file.Name), overwrite: true);
                    }
                    catch (IOException)
                    {
                        // 文件被占用（软件正在运行），跳过并继续
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // 权限不足，跳过
                    }
                }

                foreach (var subDir in new DirectoryInfo(src).GetDirectories())
                {
                    queue.Enqueue((subDir.FullName, Path.Combine(dst, subDir.Name)));
                }
            }
        });
    }
}

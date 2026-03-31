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

    // ===== 可选目标盘符列表（扫描系统中实际存在的非系统盘） =====
    public ObservableCollection<string> AvailableDrives { get; } = new();

    // ===== 筛选选项 =====

    [ObservableProperty]
    private bool _showSystemDriveOnly = true;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    // ===== 当前选中软件 =====

    [ObservableProperty]
    private SoftwareInfo? _selectedSoftware;

    [ObservableProperty]
    private string _targetDrive = string.Empty;

    // ===== 状态 =====

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isMoving;

    [ObservableProperty]
    private string _statusMessage = Localizer.Get("Vm.SoftwareMove.ClickScan", "点击\"扫描软件\"加载已安装软件列表");

    [ObservableProperty]
    private int _systemDriveSoftwareCount;

    [ObservableProperty]
    private string _systemDriveTotalSizeText = "--";

    private sealed record CopyResult(int CopiedFiles, int FailedFiles, string? FirstError);

    public SoftwareMoveViewModel(ISoftwareScanService softwareScanService, ISymlinkService symlinkService)
    {
        _softwareScanService = softwareScanService;
        _symlinkService = symlinkService;
        LoadDrives();
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
                if (drive.DriveType == DriveType.Fixed &&
                    !drive.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                {
                    AvailableDrives.Add(drive.Name.TrimEnd('\\'));
                }
            }
        }
        catch
        {
        }

        TargetDrive = AvailableDrives.Count > 0 ? AvailableDrives[0] : "D:";
    }

    /// <summary>
    /// 扫描已安装的软件列表
    /// </summary>
    [RelayCommand]
    private async Task ScanSoftwareAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        StatusMessage = Localizer.Get("Vm.SoftwareMove.Scanning", "正在扫描已安装软件...");

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

            StatusMessage = Localizer.Format("Vm.SoftwareMove.ScanCompleted", "共找到 {0} 个软件（{1} 在C盘）", SoftwareList.Count, SystemDriveTotalSizeText);
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.SoftwareMove.ScanError", "扫描出错：{0}", ex.Message);
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

        string sourcePath = SelectedSoftware.InstallLocation.TrimEnd('\\');
        if (_symlinkService.IsJunction(sourcePath))
        {
            SelectedSoftware.CanMigrate = false;
            SelectedSoftware.MigratedToPath ??= _symlinkService.GetJunctionTarget(sourcePath) ?? "(Junction)";
            StatusMessage = Localizer.Format("Vm.SoftwareMove.AlreadyMigrated", "{0} 已经是搬家后的链接目录，无需重复搬家", SelectedSoftware.DisplayName);
            return;
        }

        if (!SelectedSoftware.CanMigrate)
        {
            StatusMessage = Localizer.Get("Vm.SoftwareMove.CannotMigrate", "该软件无法迁移（安装路径不明确或已迁移）");
            return;
        }

        if (string.IsNullOrWhiteSpace(TargetDrive))
        {
            StatusMessage = Localizer.Get("Vm.SoftwareMove.SelectTargetDrive", "请先选择目标盘符");
            return;
        }

        if (!DriveInfo.GetDrives().Any(d => d.Name.StartsWith(TargetDrive, StringComparison.OrdinalIgnoreCase)))
        {
            StatusMessage = Localizer.Format("Vm.SoftwareMove.TargetDriveNotExist", "目标盘 {0} 不存在，请重新选择", TargetDrive);
            return;
        }

        IsMoving = true;
        StatusMessage = Localizer.Format("Vm.SoftwareMove.Migrating", "正在迁移：{0}...", SelectedSoftware.DisplayName);

        try
        {
            if (!AdminHelper.IsRunningAsAdmin())
            {
                StatusMessage = Localizer.Get("Vm.SoftwareMove.RequireAdmin", "请以管理员身份运行后再执行软件搬家");
                return;
            }

            if (!Directory.Exists(sourcePath))
            {
                StatusMessage = Localizer.Get("Vm.SoftwareMove.SourceNotFound", "源安装目录不存在，无法迁移");
                return;
            }

            string folderName = Path.GetFileName(sourcePath);
            string driveRoot = TargetDrive.EndsWith('\\') ? TargetDrive : TargetDrive + "\\";
            string targetPath = Path.Combine(driveRoot, "Programs", folderName);

            // 确保目标目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            // 复制软件文件到目标路径
            StatusMessage = Localizer.Format("Vm.SoftwareMove.CopyingTo", "正在复制文件：{0} → {1}", SelectedSoftware.DisplayName, targetPath);
            var copyProgress = new Progress<(int Copied, string CurrentFile)>(p =>
            {
                if (p.Copied % 20 == 0)
                {
                    string fileName = Path.GetFileName(p.CurrentFile);
                    StatusMessage = Localizer.Format("Vm.SoftwareMove.CopyingProgress", "正在复制文件：{0}（已复制 {1} 个，当前：{2}）", SelectedSoftware.DisplayName, p.Copied, fileName);
                }
            });
            var copyResult = await CopySoftwareFilesAsync(sourcePath, targetPath, copyProgress);

            if (copyResult.FailedFiles > 0)
            {
                throw new InvalidOperationException(
                    Localizer.Format("Vm.SoftwareMove.CopyFailed", "复制失败：共有 {0} 个文件未复制。{1}", copyResult.FailedFiles, copyResult.FirstError));
            }

            StatusMessage = Localizer.Format("Vm.SoftwareMove.CopyCompleted", "复制完成，共 {0} 个文件，正在验证符号链接能力...", copyResult.CopiedFiles);

            // 先验证能否创建 Junction 链接（用临时路径测试），避免删除源目录后链接创建失败
            StatusMessage = Localizer.Get("Vm.SoftwareMove.VerifyingSymlink", "正在验证符号链接能力...");
            string tempJunction = sourcePath + "__slim_test__";
            bool testSuccess = await _symlinkService.CreateJunctionAsync(tempJunction, targetPath);
            if (testSuccess)
            {
                await _symlinkService.DeleteJunctionAsync(tempJunction);
            }
            else
            {
                try { Directory.Delete(targetPath, recursive: true); } catch { }
                StatusMessage = Localizer.Get("Vm.SoftwareMove.CreateLinkFailed", "创建符号链接失败，请确认管理员权限和目标路径权限");
                return;
            }

            // 链接可创建，删除源目录，建立正式 Junction
            StatusMessage = Localizer.Get("Vm.SoftwareMove.CreatingFinalLink", "正在删除原目录并创建正式链接...");
            Directory.Delete(sourcePath, recursive: true);
            bool finalLinkCreated = await _symlinkService.CreateJunctionAsync(sourcePath, targetPath);
            if (!finalLinkCreated)
                throw new InvalidOperationException(Localizer.Get("Vm.SoftwareMove.CreateFinalLinkFailed", "正式符号链接创建失败，已复制文件请手动检查"));

            SelectedSoftware.MigratedToPath = targetPath;
            SelectedSoftware.CanMigrate = false;
            StatusMessage = Localizer.Format("Vm.SoftwareMove.MigrateCompleted", "{0} 已迁移到 {1}", SelectedSoftware.DisplayName, targetPath);
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.SoftwareMove.MigrateFailed", "迁移失败：{0}", ex.Message);
        }
        finally
        {
            IsMoving = false;
        }
    }

    /// <summary>
    /// 复制软件文件到目标路径（使用队列避免深层递归）
    /// </summary>
    private static async Task<CopyResult> CopySoftwareFilesAsync(
        string source,
        string destination,
        IProgress<(int Copied, string CurrentFile)>? progress = null)
    {
        return await Task.Run(() =>
        {
            var queue = new Queue<(string src, string dst)>();
            queue.Enqueue((source, destination));

            int copiedFiles = 0;
            int failedFiles = 0;
            string? firstError = null;

            while (queue.Count > 0)
            {
                var (src, dst) = queue.Dequeue();

                try
                {
                    Directory.CreateDirectory(dst);
                }
                catch (Exception ex)
                {
                    failedFiles++;
                    firstError ??= Localizer.Format("Vm.SoftwareMove.CreateDirFailed", "无法创建目录：{0}，{1}", dst, ex.Message);
                    continue;
                }

                IEnumerable<FileInfo> files;
                try
                {
                    files = new DirectoryInfo(src).GetFiles();
                }
                catch (Exception ex)
                {
                    failedFiles++;
                    firstError ??= Localizer.Format("Vm.SoftwareMove.ReadDirFailed", "无法读取目录：{0}，{1}", src, ex.Message);
                    continue;
                }

                foreach (var file in files)
                {
                    try
                    {
                        file.CopyTo(Path.Combine(dst, file.Name), overwrite: true);
                        copiedFiles++;
                        progress?.Report((copiedFiles, file.FullName));
                    }
                    catch (Exception ex)
                    {
                        failedFiles++;
                        firstError ??= Localizer.Format("Vm.SoftwareMove.CopyFileFailed", "无法复制文件：{0}，{1}", file.FullName, ex.Message);
                    }
                }

                DirectoryInfo[] subDirs;
                try
                {
                    subDirs = new DirectoryInfo(src).GetDirectories();
                }
                catch (Exception ex)
                {
                    failedFiles++;
                    firstError ??= Localizer.Format("Vm.SoftwareMove.ReadSubDirFailed", "无法读取子目录：{0}，{1}", src, ex.Message);
                    continue;
                }

                foreach (var subDir in subDirs)
                    queue.Enqueue((subDir.FullName, Path.Combine(dst, subDir.Name)));
            }

            return new CopyResult(copiedFiles, failedFiles, firstError);
        });
    }
}

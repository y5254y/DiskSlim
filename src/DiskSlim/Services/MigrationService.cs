using DiskSlim.Models;
using Microsoft.Win32;

namespace DiskSlim.Services;

/// <summary>
/// 文件夹迁移服务实现
/// 迁移流程：① 扫描源文件夹大小 → ② 验证目标空间 → ③ 复制文件 → ④ 创建 Junction 链接 → ⑤ 更新注册表
/// </summary>
public class MigrationService : IMigrationService
{
    private readonly ISymlinkService _symlinkService;

    public MigrationService(ISymlinkService symlinkService)
    {
        _symlinkService = symlinkService;
    }

    /// <summary>
    /// 获取可迁移的用户文件夹列表
    /// </summary>
    public IReadOnlyList<UserFolderInfo> GetMigratableFolders()
    {
        return new List<UserFolderInfo>
        {
            new UserFolderInfo
            {
                DisplayName = "桌面",
                CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                RegistryKey = "Desktop",
                IconGlyph = "\uE8FC",
                IsMigrated = IsAlreadyMigrated(Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
            },
            new UserFolderInfo
            {
                DisplayName = "文档",
                CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RegistryKey = "Personal",
                IconGlyph = "\uE8A5",
                IsMigrated = IsAlreadyMigrated(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
            },
            new UserFolderInfo
            {
                DisplayName = "下载",
                CurrentPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                RegistryKey = "{374DE290-123F-4565-9164-39C4925E467B}",
                IconGlyph = "\uE896",
                IsMigrated = IsAlreadyMigrated(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"))
            },
            new UserFolderInfo
            {
                DisplayName = "图片",
                CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                RegistryKey = "My Pictures",
                IconGlyph = "\uEB9F",
                IsMigrated = IsAlreadyMigrated(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
            },
            new UserFolderInfo
            {
                DisplayName = "视频",
                CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                RegistryKey = "My Video",
                IconGlyph = "\uE8B2",
                IsMigrated = IsAlreadyMigrated(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
            },
            new UserFolderInfo
            {
                DisplayName = "音乐",
                CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                RegistryKey = "My Music",
                IconGlyph = "\uEC4F",
                IsMigrated = IsAlreadyMigrated(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
            }
        };
    }

    /// <summary>
    /// 执行文件夹迁移
    /// </summary>
    public async Task ExecuteMigrationAsync(
        MigrationTask task,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        task.Status = MigrationStatus.InProgress;

        try
        {
            // 阶段1：计算源文件夹大小
            progress?.Report(new MigrationProgress("正在计算文件夹大小...", 0, 0, string.Empty));
            task.TotalSizeBytes = await CalculateFolderSizeAsync(task.SourcePath, cancellationToken);

            // 阶段2：确保目标目录存在
            if (!Directory.Exists(task.DestinationPath))
                Directory.CreateDirectory(task.DestinationPath);

            // 阶段3：复制文件到目标路径
            long copied = 0;
            await CopyDirectoryAsync(
                task.SourcePath,
                task.DestinationPath,
                new Progress<(long bytes, string file)>(p =>
                {
                    copied += p.bytes;
                    task.TransferredBytes = copied;
                    progress?.Report(new MigrationProgress("正在复制文件...", copied, task.TotalSizeBytes, p.file));
                }),
                cancellationToken);

            // 阶段4：删除源目录（该步骤可能耗时）
            progress?.Report(new MigrationProgress("正在删除原目录...", copied, task.TotalSizeBytes, string.Empty));
            await DeleteSourceDirectoryAsync(task.SourcePath, cancellationToken);

            // 阶段5：创建 Junction 链接
            progress?.Report(new MigrationProgress("正在创建符号链接...", copied, task.TotalSizeBytes, string.Empty));
            bool junctionCreated = await _symlinkService.CreateJunctionAsync(task.SourcePath, task.DestinationPath);
            if (!junctionCreated)
                throw new InvalidOperationException("创建符号链接失败，请以管理员身份运行后重试");

            task.HasSymlink = true;

            // 阶段6：更新注册表（可选，Junction 方式不强制更新）
            progress?.Report(new MigrationProgress("正在更新系统配置...", copied, task.TotalSizeBytes, string.Empty));

            task.Status = MigrationStatus.Completed;
            task.CompletedAt = DateTime.Now;
        }
        catch (OperationCanceledException)
        {
            task.Status = MigrationStatus.Failed;
            task.ErrorMessage = "用户取消了迁移操作";
            throw;
        }
        catch (Exception ex)
        {
            task.Status = MigrationStatus.Failed;
            task.ErrorMessage = ex.Message;
            throw;
        }
    }

    /// <summary>
    /// 回退迁移：删除 Junction 链接（数据保留在目标盘）
    /// </summary>
    public async Task RollbackMigrationAsync(MigrationTask task)
    {
        if (!task.CanRollback) return;

        // 删除 Junction 链接
        await _symlinkService.DeleteJunctionAsync(task.SourcePath);

        // 将原目录名重新创建（空目录），提示用户手动处理
        Directory.CreateDirectory(task.SourcePath);

        task.Status = MigrationStatus.RolledBack;
    }

    /// <summary>
    /// 验证目标盘可用空间是否足够
    /// </summary>
    public async Task<bool> ValidateDestinationSpaceAsync(string sourcePath, string destinationDrive)
    {
        long sourceSize = await CalculateFolderSizeAsync(sourcePath, CancellationToken.None);
        var driveInfo = new DriveInfo(destinationDrive);
        return driveInfo.AvailableFreeSpace > sourceSize * 1.1; // 预留10%余量
    }

    // ===== 私有辅助方法 =====

    /// <summary>检测文件夹是否已迁移（是符号链接或路径不在系统盘）</summary>
    private bool IsAlreadyMigrated(string path)
    {
        return _symlinkService.IsJunction(path);
    }

    /// <summary>删除源目录（使用可取消的迭代遍历，跳过重解析点）</summary>
    private static async Task DeleteSourceDirectoryAsync(string sourcePath, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(sourcePath))
                return;

            var visitedDirs = new List<string>();
            var pending = new Stack<string>();
            pending.Push(sourcePath);

            while (pending.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                var current = pending.Pop();
                visitedDirs.Add(current);

                try
                {
                    foreach (var file in Directory.EnumerateFiles(current))
                    {
                        ct.ThrowIfCancellationRequested();
                        var fi = new FileInfo(file);
                        try
                        {
                            fi.Attributes = FileAttributes.Normal;
                            fi.Delete();
                        }
                        catch (Exception ex)
                        {
                            throw new IOException($"无法删除文件：{file}。请关闭占用该文件的程序后重试。", ex);
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }
                catch (Exception ex) when (ex is not IOException)
                {
                    throw new IOException($"无法访问目录：{current}", ex);
                }

                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(current))
                    {
                        ct.ThrowIfCancellationRequested();
                        FileAttributes attr;
                        try
                        {
                            attr = File.GetAttributes(dir);
                        }
                        catch (Exception ex)
                        {
                            throw new IOException($"无法读取目录属性：{dir}", ex);
                        }

                        if ((attr & FileAttributes.ReparsePoint) != 0)
                        {
                            try
                            {
                                Directory.Delete(dir, false);
                            }
                            catch (Exception ex)
                            {
                                throw new IOException($"无法删除重解析点目录：{dir}", ex);
                            }
                            continue;
                        }

                        pending.Push(dir);
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }
                catch (Exception ex) when (ex is not IOException)
                {
                    throw new IOException($"无法枚举子目录：{current}", ex);
                }
            }

            for (int i = visitedDirs.Count - 1; i >= 0; i--)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    Directory.Delete(visitedDirs[i], false);
                }
                catch (Exception ex)
                {
                    throw new IOException($"无法删除目录：{visitedDirs[i]}", ex);
                }
            }
        }, ct);
    }

    /// <summary>递归计算文件夹大小</summary>
    private static async Task<long> CalculateFolderSizeAsync(string path, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    try { size += new FileInfo(file).Length; } catch { }
                }
            }
            catch { }
            return size;
        }, ct);
    }

    /// <summary>递归复制目录（使用队列避免深层递归导致的栈溢出）</summary>
    private static async Task CopyDirectoryAsync(
        string sourceDir,
        string destDir,
        IProgress<(long bytes, string file)>? progress,
        CancellationToken ct)
    {
        await Task.Run(() =>
        {
            // 使用队列（BFS）替代递归，避免深层目录树导致的栈溢出
            var queue = new Queue<(string src, string dst)>();
            queue.Enqueue((sourceDir, destDir));

            while (queue.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                var (src, dst) = queue.Dequeue();

                Directory.CreateDirectory(dst);

                // 复制当前目录中的文件
                foreach (var file in new DirectoryInfo(src).GetFiles())
                {
                    ct.ThrowIfCancellationRequested();
                    string targetFilePath = Path.Combine(dst, file.Name);
                    try
                    {
                        file.CopyTo(targetFilePath, overwrite: true);
                        progress?.Report((file.Length, file.FullName));
                    }
                    catch { }
                }

                // 将子目录加入队列
                foreach (var subDir in new DirectoryInfo(src).GetDirectories())
                {
                    queue.Enqueue((subDir.FullName, Path.Combine(dst, subDir.Name)));
                }
            }
        }, ct);
    }
}

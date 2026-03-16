using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 磁盘扫描服务实现，高性能异步遍历文件系统
/// </summary>
public class DiskScanService : IDiskScanService
{
    /// <summary>
    /// 获取所有物理磁盘基础信息
    /// </summary>
    public IReadOnlyList<DriveInfoModel> GetAllDrives()
    {
        var result = new List<DriveInfoModel>();
        string systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;

            result.Add(new DriveInfoModel
            {
                DriveLetter = drive.Name.TrimEnd('\\'),
                VolumeLabel = drive.VolumeLabel,
                TotalBytes = drive.TotalSize,
                FreeBytes = drive.TotalFreeSpace,
                UsedBytes = drive.TotalSize - drive.TotalFreeSpace,
                FileSystem = drive.DriveFormat,
                IsSystemDrive = drive.Name.StartsWith(systemDrive, StringComparison.OrdinalIgnoreCase)
            });
        }

        return result;
    }

    /// <summary>
    /// 异步扫描磁盘，统计空间使用情况
    /// </summary>
    public async Task<DriveInfoModel> ScanDriveAsync(
        string drivePath,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 先获取磁盘基础信息
        string systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
        var di = new DriveInfo(drivePath.Substring(0, 1));

        var model = new DriveInfoModel
        {
            DriveLetter = di.Name.TrimEnd('\\'),
            VolumeLabel = di.VolumeLabel,
            TotalBytes = di.TotalSize,
            FreeBytes = di.TotalFreeSpace,
            UsedBytes = di.TotalSize - di.TotalFreeSpace,
            FileSystem = di.DriveFormat,
            IsSystemDrive = drivePath.StartsWith(systemDrive, StringComparison.OrdinalIgnoreCase),
            ScanTime = DateTime.Now
        };

        return await Task.FromResult(model);
    }

    /// <summary>
    /// 异步扫描文件夹，返回按大小排序的 Top N 子项
    /// </summary>
    public async Task<IReadOnlyList<FileItem>> ScanTopItemsAsync(
        string folderPath,
        int maxItems = 100,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var items = new List<FileItem>();
        long filesScanned = 0;
        long bytesScanned = 0;

        await Task.Run(() =>
        {
            // 扫描一级子目录大小
            try
            {
                var dirInfo = new DirectoryInfo(folderPath);

                foreach (var subDir in dirInfo.EnumerateDirectories())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        long dirSize = CalculateDirectorySize(subDir, ref filesScanned, cancellationToken);
                        bytesScanned += dirSize;

                        items.Add(new FileItem
                        {
                            FullPath = subDir.FullName,
                            SizeBytes = dirSize,
                            LastModified = subDir.LastWriteTime,
                            LastAccessed = subDir.LastAccessTime,
                            IsDirectory = true
                        });

                        progress?.Report(new ScanProgress(filesScanned, bytesScanned, subDir.FullName));
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                }

                // 扫描直接文件
                foreach (var file in dirInfo.EnumerateFiles())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        filesScanned++;
                        bytesScanned += file.Length;

                        items.Add(new FileItem
                        {
                            FullPath = file.FullName,
                            SizeBytes = file.Length,
                            LastModified = file.LastWriteTime,
                            LastAccessed = file.LastAccessTime,
                            IsDirectory = false
                        });
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }, cancellationToken);

        // 按大小降序排序，取前 N 条
        items.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));

        // 计算占比
        if (bytesScanned > 0)
        {
            foreach (var item in items)
            {
                item.SizePercent = (double)item.SizeBytes / bytesScanned;
            }
        }

        return items.Take(maxItems).ToList();
    }

    /// <summary>
    /// 递归计算文件夹总大小（字节）
    /// </summary>
    private static long CalculateDirectorySize(
        DirectoryInfo dir,
        ref long filesScanned,
        CancellationToken cancellationToken)
    {
        long size = 0;

        try
        {
            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    size += file.Length;
                    Interlocked.Increment(ref filesScanned);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }

        return size;
    }
}

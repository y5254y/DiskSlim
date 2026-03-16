using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 磁盘扫描服务接口，负责异步扫描磁盘/文件夹并统计空间使用情况
/// </summary>
public interface IDiskScanService
{
    /// <summary>
    /// 异步扫描指定磁盘或文件夹，返回磁盘信息
    /// </summary>
    /// <param name="drivePath">扫描路径（如 "C:\" 或 "C:\Users"）</param>
    /// <param name="progress">进度回调，报告已扫描文件数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<DriveInfoModel> ScanDriveAsync(
        string drivePath,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步扫描文件夹，返回按大小排序的子项列表（用于大文件排行）
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    /// <param name="maxItems">最多返回条目数</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<IReadOnlyList<FileItem>> ScanTopItemsAsync(
        string folderPath,
        int maxItems = 100,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有磁盘的基础信息（不扫描文件）
    /// </summary>
    IReadOnlyList<DriveInfoModel> GetAllDrives();
}

/// <summary>
/// 扫描进度报告数据
/// </summary>
public record ScanProgress(
    long FilesScanned,
    long BytesScanned,
    string CurrentPath);

using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 清理引擎服务接口，负责扫描可清理空间和执行清理操作
/// </summary>
public interface ICleanupService
{
    /// <summary>
    /// 获取所有可清理项目的列表（含安全等级和描述）
    /// </summary>
    IReadOnlyList<CleanupItem> GetCleanupItems();

    /// <summary>
    /// 异步扫描各清理项的可释放空间大小
    /// </summary>
    /// <param name="items">要扫描的清理项列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ScanEstimatedSizesAsync(
        IEnumerable<CleanupItem> items,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步执行清理操作（仅清理已选中的项目）
    /// </summary>
    /// <param name="items">要清理的项目列表</param>
    /// <param name="progress">进度回调（报告已清理字节数）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实际释放的总字节数</returns>
    Task<long> CleanAsync(
        IEnumerable<CleanupItem> items,
        IProgress<CleanupProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 清理进度报告数据
/// </summary>
public record CleanupProgress(
    string CurrentItemName,
    long BytesCleaned,
    int ItemsCompleted,
    int ItemsTotal);

using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 磁盘扫描快照服务接口，负责保存、读取和对比历史快照
/// </summary>
public interface ISnapshotService
{
    /// <summary>
    /// 初始化数据库（建表等），应在应用启动时调用
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 保存一次扫描快照到数据库
    /// </summary>
    /// <param name="snapshot">要保存的快照数据</param>
    Task SaveSnapshotAsync(DiskSnapshot snapshot);

    /// <summary>
    /// 获取所有历史快照列表（按时间倒序，不含文件夹明细）
    /// </summary>
    Task<IReadOnlyList<DiskSnapshot>> GetSnapshotsAsync();

    /// <summary>
    /// 获取指定快照的完整数据（含文件夹明细）
    /// </summary>
    /// <param name="snapshotId">快照 ID</param>
    Task<DiskSnapshot?> GetSnapshotWithDetailsAsync(int snapshotId);

    /// <summary>
    /// 删除指定快照
    /// </summary>
    /// <param name="snapshotId">快照 ID</param>
    Task DeleteSnapshotAsync(int snapshotId);

    /// <summary>
    /// 对比两次快照，返回各文件夹的空间变化
    /// </summary>
    /// <param name="oldSnapshotId">旧快照 ID</param>
    /// <param name="newSnapshotId">新快照 ID</param>
    Task<IReadOnlyList<SnapshotDiffItem>> CompareSnapshotsAsync(int oldSnapshotId, int newSnapshotId);

    /// <summary>
    /// 从当前磁盘状态创建一个新快照（扫描C盘顶层文件夹）
    /// </summary>
    /// <param name="label">快照备注（可空）</param>
    /// <param name="progress">进度报告回调</param>
    /// <param name="token">取消令牌</param>
    Task<DiskSnapshot> CreateSnapshotAsync(string? label, IProgress<string>? progress, CancellationToken token);
}

using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// Compact OS 系统压缩服务接口
/// </summary>
public interface ICompactOsService
{
    /// <summary>获取当前 CompactOS 压缩状态</summary>
    Task<CompactOsStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>启用 CompactOS 系统压缩（需要管理员权限）</summary>
    Task<CompactOsResult> EnableCompactionAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>禁用 CompactOS 系统压缩（需要管理员权限）</summary>
    Task<CompactOsResult> DisableCompactionAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

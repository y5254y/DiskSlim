using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// WSL 磁盘空间回收服务接口
/// </summary>
public interface IWslService
{
    /// <summary>检测系统是否安装了 WSL</summary>
    Task<bool> IsWslInstalledAsync(CancellationToken cancellationToken = default);

    /// <summary>获取所有已安装的 WSL 发行版及其 vhdx 信息</summary>
    Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回收指定发行版的 WSL 虚拟磁盘空间
    /// （先关闭 WSL，再通过 diskpart 压缩 vhdx 文件）
    /// </summary>
    Task<WslReclaimResult> ReclaimDiskSpaceAsync(
        WslDistribution distribution,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

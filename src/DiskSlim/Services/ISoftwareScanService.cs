using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 已安装软件扫描服务接口，从注册表获取已安装软件信息
/// </summary>
public interface ISoftwareScanService
{
    /// <summary>
    /// 异步扫描所有已安装的软件
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已安装软件列表，按占用空间降序排列</returns>
    Task<IReadOnlyList<SoftwareInfo>> ScanInstalledSoftwareAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 筛选出安装在系统盘（C盘）的软件
    /// </summary>
    /// <param name="allSoftware">全部软件列表</param>
    /// <returns>安装在C盘的软件列表</returns>
    IReadOnlyList<SoftwareInfo> FilterSystemDriveSoftware(IEnumerable<SoftwareInfo> allSoftware);
}

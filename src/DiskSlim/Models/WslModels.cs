namespace DiskSlim.Models;

/// <summary>
/// WSL 发行版信息
/// </summary>
public class WslDistribution
{
    /// <summary>发行版名称（如 Ubuntu、Debian）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>虚拟磁盘 .vhdx 文件路径</summary>
    public string VhdxPath { get; set; } = string.Empty;

    /// <summary>虚拟磁盘文件当前大小（字节）</summary>
    public long VhdxSizeBytes { get; set; }

    /// <summary>发行版是否正在运行</summary>
    public bool IsRunning { get; set; }

    /// <summary>是否找到对应的 vhdx 文件</summary>
    public bool VhdxFound => !string.IsNullOrEmpty(VhdxPath);
}

/// <summary>
/// WSL 磁盘回收操作结果
/// </summary>
public record WslReclaimResult(
    /// <summary>操作是否成功</summary>
    bool IsSuccess,
    /// <summary>发行版名称</summary>
    string DistributionName,
    /// <summary>回收前文件大小（字节）</summary>
    long SizeBeforeBytes,
    /// <summary>回收后文件大小（字节）</summary>
    long SizeAfterBytes,
    /// <summary>操作输出日志</summary>
    string Output,
    /// <summary>错误信息（操作失败时）</summary>
    string? ErrorMessage = null)
{
    /// <summary>释放的磁盘空间（字节）</summary>
    public long SavedBytes => SizeBeforeBytes - SizeAfterBytes;
}

namespace DiskSlim.Models;

/// <summary>
/// 已安装软件信息模型，从注册表读取
/// </summary>
public class SoftwareInfo
{
    /// <summary>软件显示名称</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>软件版本号</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>发布者/厂商</summary>
    public string Publisher { get; set; } = string.Empty;

    /// <summary>安装路径</summary>
    public string InstallLocation { get; set; } = string.Empty;

    /// <summary>占用磁盘空间（字节）；部分软件无此信息则为 -1</summary>
    public long InstallSizeBytes { get; set; } = -1;

    /// <summary>安装日期</summary>
    public DateTime? InstallDate { get; set; }

    /// <summary>卸载命令行</summary>
    public string UninstallString { get; set; } = string.Empty;

    /// <summary>注册表键路径，用于读写</summary>
    public string RegistryKey { get; set; } = string.Empty;

    /// <summary>是否安装在C盘</summary>
    public bool IsOnSystemDrive =>
        !string.IsNullOrEmpty(InstallLocation)
        && InstallLocation.StartsWith(Environment.GetEnvironmentVariable("SystemDrive") ?? "C:", StringComparison.OrdinalIgnoreCase);

    /// <summary>是否可迁移（安装路径明确且非系统核心组件）</summary>
    public bool CanMigrate { get; set; }

    /// <summary>迁移后的目标路径（迁移完成后填充）</summary>
    public string? MigratedToPath { get; set; }
}

using DiskSlim.Helpers;

namespace DiskSlim.Models;

/// <summary>
/// 清理项目模型，代表一类可清理的空间（如临时文件、回收站等）
/// </summary>
public class CleanupItem
{
    /// <summary>清理项名称（如"系统临时文件"）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>清理项描述（这是什么？删了会怎样？）</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>安全等级（🟢Safe / 🟡Caution / 🔴Danger）</summary>
    public SafetyLevel Safety { get; set; } = SafetyLevel.Safe;

    /// <summary>预估可释放大小（字节），扫描后更新</summary>
    public long EstimatedSize { get; set; }

    /// <summary>是否被用户勾选（默认仅 Safe 级别预选）</summary>
    public bool IsSelected { get; set; }

    /// <summary>Segoe MDL2 图标字形（Unicode）</summary>
    public string IconGlyph { get; set; } = "\uE74D";

    /// <summary>是否正在扫描中</summary>
    public bool IsScanning { get; set; }

    /// <summary>是否已完成清理</summary>
    public bool IsCleaned { get; set; }

    /// <summary>
    /// 安全等级对应的徽章文字
    /// </summary>
    public string SafetyBadge => Safety switch
    {
        SafetyLevel.Safe => "🟢 安全",
        SafetyLevel.Caution => "🟡 谨慎",
        SafetyLevel.Danger => "🔴 危险",
        _ => string.Empty
    };

    /// <summary>
    /// 执行实际清理的委托，由 CleanupService 填充
    /// </summary>
    public Func<IProgress<long>, CancellationToken, Task<long>>? CleanAction { get; set; }
}

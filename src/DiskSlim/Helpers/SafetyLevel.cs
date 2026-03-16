namespace DiskSlim.Helpers;

/// <summary>
/// 清理操作的安全等级枚举
/// </summary>
public enum SafetyLevel
{
    /// <summary>
    /// 🟢 安全：完全安全，可以放心清理（如临时文件、回收站、日志）
    /// </summary>
    Safe,

    /// <summary>
    /// 🟡 谨慎：建议清理但需用户确认（如 Windows Update 残留、浏览器缓存）
    /// </summary>
    Caution,

    /// <summary>
    /// 🔴 危险：谨慎操作，可能影响系统功能（如休眠文件、虚拟内存）
    /// </summary>
    Danger
}

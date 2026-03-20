namespace DiskSlim.Models;

/// <summary>
/// CompactOS 压缩状态信息
/// </summary>
public record CompactOsStatus(
    /// <summary>系统当前是否处于压缩状态</summary>
    bool IsCompressed,
    /// <summary>原始查询输出文本</summary>
    string RawOutput,
    /// <summary>查询是否成功</summary>
    bool IsSuccess,
    /// <summary>错误信息（查询失败时）</summary>
    string? ErrorMessage = null);

/// <summary>
/// CompactOS 操作结果
/// </summary>
public record CompactOsResult(
    /// <summary>操作是否成功</summary>
    bool IsSuccess,
    /// <summary>操作输出日志</summary>
    string Output,
    /// <summary>估算释放/恢复的字节数（启用时为正，禁用时为负）</summary>
    long EstimatedSavedBytes,
    /// <summary>错误信息（操作失败时）</summary>
    string? ErrorMessage = null);

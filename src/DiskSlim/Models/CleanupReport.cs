namespace DiskSlim.Models;

/// <summary>
/// 一次清理操作的详细报告
/// </summary>
public class CleanupReport
{
    /// <summary>报告唯一 ID（自增主键）</summary>
    public int Id { get; set; }

    /// <summary>清理开始时间</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>清理完成时间</summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>总共释放的字节数</summary>
    public long TotalFreedBytes { get; set; }

    /// <summary>清理的各项明细列表</summary>
    public List<CleanupReportItem> Items { get; set; } = new();

    /// <summary>格式化后的总释放空间文字（如 "1.23 GB"）</summary>
    public string TotalFreedText => Helpers.FileSizeHelper.Format(TotalFreedBytes);

    /// <summary>清理完成时间的友好显示文字</summary>
    public string CompletedAtText => CompletedAt.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>清理耗时</summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>耗时友好显示</summary>
    public string DurationText => Duration.TotalSeconds < 60
        ? $"{Duration.TotalSeconds:F1} 秒"
        : $"{(int)Duration.TotalMinutes} 分 {Duration.Seconds} 秒";
}

/// <summary>
/// 清理报告中的单项明细
/// </summary>
public class CleanupReportItem
{
    /// <summary>明细 ID</summary>
    public int Id { get; set; }

    /// <summary>所属报告 ID</summary>
    public int ReportId { get; set; }

    /// <summary>清理项名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>实际释放字节数</summary>
    public long FreedBytes { get; set; }

    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>失败原因（可选）</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>格式化后的释放空间文字</summary>
    public string FreedText => Helpers.FileSizeHelper.Format(FreedBytes);
}

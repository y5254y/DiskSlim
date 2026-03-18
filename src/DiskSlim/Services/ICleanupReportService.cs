using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 清理报告服务接口，负责持久化存储和检索历史清理报告
/// </summary>
public interface ICleanupReportService
{
    /// <summary>
    /// 保存一份清理报告到数据库
    /// </summary>
    /// <param name="report">要保存的报告</param>
    Task SaveReportAsync(CleanupReport report);

    /// <summary>
    /// 获取所有历史清理报告（按时间倒序）
    /// </summary>
    /// <param name="limit">最大返回数量，默认50条</param>
    Task<IReadOnlyList<CleanupReport>> GetReportsAsync(int limit = 50);

    /// <summary>
    /// 删除指定报告
    /// </summary>
    /// <param name="reportId">报告 ID</param>
    Task DeleteReportAsync(int reportId);

    /// <summary>
    /// 将报告导出为 TXT 格式内容
    /// </summary>
    string ExportToText(CleanupReport report);

    /// <summary>
    /// 将报告导出为 HTML 格式内容
    /// </summary>
    string ExportToHtml(CleanupReport report);

    /// <summary>
    /// 将报告导出为 CSV 格式内容
    /// </summary>
    string ExportToCsv(CleanupReport report);

    /// <summary>
    /// 初始化数据库（建表等），应在应用启动时调用
    /// </summary>
    Task InitializeAsync();
}

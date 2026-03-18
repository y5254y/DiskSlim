using DiskSlim.Models;
using Microsoft.Data.Sqlite;
using System.Text;

namespace DiskSlim.Services;

/// <summary>
/// 清理报告服务实现，使用 SQLite 持久化存储清理历史
/// </summary>
public class CleanupReportService : ICleanupReportService
{
    private readonly string _dbPath;

    public CleanupReportService()
    {
        // 数据库存储在用户应用数据目录
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DiskSlim");
        Directory.CreateDirectory(appDataDir);
        _dbPath = Path.Combine(appDataDir, "reports.db");
    }

    /// <summary>
    /// 初始化数据库，创建表结构
    /// </summary>
    public async Task InitializeAsync()
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS CleanupReports (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                StartedAt    TEXT    NOT NULL,
                CompletedAt  TEXT    NOT NULL,
                TotalFreedBytes INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS CleanupReportItems (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                ReportId     INTEGER NOT NULL REFERENCES CleanupReports(Id) ON DELETE CASCADE,
                Name         TEXT    NOT NULL,
                FreedBytes   INTEGER NOT NULL DEFAULT 0,
                Success      INTEGER NOT NULL DEFAULT 1,
                ErrorMessage TEXT
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 保存清理报告（报告和明细）
    /// </summary>
    public async Task SaveReportAsync(CleanupReport report)
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            // 插入报告主记录
            var insertReport = conn.CreateCommand();
            insertReport.Transaction = (SqliteTransaction)tx;
            insertReport.CommandText = """
                INSERT INTO CleanupReports (StartedAt, CompletedAt, TotalFreedBytes)
                VALUES ($started, $completed, $total);
                SELECT last_insert_rowid();
                """;
            insertReport.Parameters.AddWithValue("$started", report.StartedAt.ToString("o"));
            insertReport.Parameters.AddWithValue("$completed", report.CompletedAt.ToString("o"));
            insertReport.Parameters.AddWithValue("$total", report.TotalFreedBytes);

            var idObj = await insertReport.ExecuteScalarAsync();
            int reportId = System.Convert.ToInt32(idObj);
            report.Id = reportId;

            // 插入明细
            foreach (var item in report.Items)
            {
                var insertItem = conn.CreateCommand();
                insertItem.Transaction = (SqliteTransaction)tx;
                insertItem.CommandText = """
                    INSERT INTO CleanupReportItems (ReportId, Name, FreedBytes, Success, ErrorMessage)
                    VALUES ($rid, $name, $freed, $success, $err);
                    """;
                insertItem.Parameters.AddWithValue("$rid", reportId);
                insertItem.Parameters.AddWithValue("$name", item.Name);
                insertItem.Parameters.AddWithValue("$freed", item.FreedBytes);
                insertItem.Parameters.AddWithValue("$success", item.Success ? 1 : 0);
                insertItem.Parameters.AddWithValue("$err", item.ErrorMessage ?? (object)DBNull.Value);
                await insertItem.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 读取历史报告列表
    /// </summary>
    public async Task<IReadOnlyList<CleanupReport>> GetReportsAsync(int limit = 50)
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        // 读取报告主记录
        var reports = new List<CleanupReport>();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, StartedAt, CompletedAt, TotalFreedBytes
            FROM CleanupReports
            ORDER BY Id DESC
            LIMIT $limit;
            """;
        cmd.Parameters.AddWithValue("$limit", limit);

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                reports.Add(new CleanupReport
                {
                    Id = reader.GetInt32(0),
                    StartedAt = DateTime.Parse(reader.GetString(1)),
                    CompletedAt = DateTime.Parse(reader.GetString(2)),
                    TotalFreedBytes = reader.GetInt64(3)
                });
            }
        }

        // 为每份报告读取明细
        foreach (var report in reports)
        {
            var itemCmd = conn.CreateCommand();
            itemCmd.CommandText = """
                SELECT Id, Name, FreedBytes, Success, ErrorMessage
                FROM CleanupReportItems
                WHERE ReportId = $rid;
                """;
            itemCmd.Parameters.AddWithValue("$rid", report.Id);

            await using var itemReader = await itemCmd.ExecuteReaderAsync();
            while (await itemReader.ReadAsync())
            {
                report.Items.Add(new CleanupReportItem
                {
                    Id = itemReader.GetInt32(0),
                    ReportId = report.Id,
                    Name = itemReader.GetString(1),
                    FreedBytes = itemReader.GetInt64(2),
                    Success = itemReader.GetInt32(3) == 1,
                    ErrorMessage = itemReader.IsDBNull(4) ? null : itemReader.GetString(4)
                });
            }
        }

        return reports;
    }

    /// <summary>
    /// 删除指定报告及其明细
    /// </summary>
    public async Task DeleteReportAsync(int reportId)
    {
        await using var conn = new SqliteConnection($"Data Source={_dbPath}");
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM CleanupReports WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", reportId);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 将报告导出为纯文本格式
    /// </summary>
    public string ExportToText(CleanupReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("  DiskSlim - C盘瘦身大师  清理报告");
        sb.AppendLine("========================================");
        sb.AppendLine($"清理时间：{report.CompletedAtText}");
        sb.AppendLine($"清理耗时：{report.DurationText}");
        sb.AppendLine($"总释放空间：{report.TotalFreedText}");
        sb.AppendLine();
        sb.AppendLine("清理明细：");
        sb.AppendLine("----------------------------------------");
        foreach (var item in report.Items)
        {
            string status = item.Success ? "✓" : "✗";
            sb.AppendLine($"  {status} {item.Name,-30} {item.FreedText,10}");
            if (!item.Success && item.ErrorMessage != null)
                sb.AppendLine($"     错误：{item.ErrorMessage}");
        }
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"合计释放：{report.TotalFreedText}");
        sb.AppendLine();
        sb.AppendLine("由 DiskSlim - C盘瘦身大师 生成");
        return sb.ToString();
    }

    /// <summary>
    /// 将报告导出为 HTML 格式
    /// </summary>
    public string ExportToHtml(CleanupReport report)
    {
        var rows = new StringBuilder();
        foreach (var item in report.Items)
        {
            string rowClass = item.Success ? "" : " style=\"color:#c00\"";
            string statusIcon = item.Success ? "✓" : "✗";
            rows.AppendLine($"""
                <tr{rowClass}>
                  <td>{System.Net.WebUtility.HtmlEncode(item.Name)}</td>
                  <td style="text-align:right">{System.Net.WebUtility.HtmlEncode(item.FreedText)}</td>
                  <td style="text-align:center">{statusIcon}</td>
                </tr>
                """);
        }

        return $$"""
            <!DOCTYPE html>
            <html lang="zh-CN">
            <head>
              <meta charset="UTF-8"/>
              <title>DiskSlim 清理报告</title>
              <style>
                body { font-family: "Microsoft YaHei", sans-serif; padding: 24px; background: #f5f5f5; }
                h1 { color: #0078d4; }
                .meta { color: #555; margin-bottom: 16px; }
                table { border-collapse: collapse; width: 100%; background: #fff; box-shadow: 0 2px 4px rgba(0,0,0,.1); }
                th, td { padding: 10px 14px; border: 1px solid #e0e0e0; }
                th { background: #0078d4; color: #fff; }
                .total { font-weight: bold; font-size: 1.2em; margin-top: 16px; color: #0078d4; }
              </style>
            </head>
            <body>
              <h1>🧹 DiskSlim 清理报告</h1>
              <p class="meta">
                清理时间：{{System.Net.WebUtility.HtmlEncode(report.CompletedAtText)}}&nbsp;&nbsp;
                耗时：{{System.Net.WebUtility.HtmlEncode(report.DurationText)}}
              </p>
              <table>
                <thead>
                  <tr><th>清理项目</th><th>释放空间</th><th>状态</th></tr>
                </thead>
                <tbody>
            {{rows}}
                </tbody>
              </table>
              <p class="total">总共释放：{{System.Net.WebUtility.HtmlEncode(report.TotalFreedText)}}</p>
              <p style="color:#aaa;font-size:.85em">由 DiskSlim - C盘瘦身大师 生成</p>
            </body>
            </html>
            """;
    }
}

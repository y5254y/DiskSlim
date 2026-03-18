using System.Diagnostics;

namespace DiskSlim.Services;

/// <summary>
/// 定时扫描服务实现，通过 schtasks.exe 操作 Windows 任务计划程序
/// </summary>
public class ScheduleService : IScheduleService
{
    private const string TaskName = "DiskSlim_AutoScan";

    /// <summary>
    /// 注册定时扫描任务（使用 schtasks.exe 命令行工具）
    /// </summary>
    public async Task RegisterScheduledTaskAsync(ScanSchedule schedule, TimeSpan triggerTime)
    {
        string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        if (string.IsNullOrEmpty(exePath)) return;

        string scheduleArg = schedule switch
        {
            ScanSchedule.Daily => "DAILY",
            ScanSchedule.Weekly => "WEEKLY",
            ScanSchedule.Monthly => "MONTHLY",
            _ => "DAILY"
        };

        string timeStr = $"{triggerTime.Hours:D2}:{triggerTime.Minutes:D2}";

        // 先移除旧任务
        await UnregisterScheduledTaskAsync();

        // 创建新任务
        string args = $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --autoscan\" " +
                      $"/sc {scheduleArg} /st {timeStr} /f /ru INTERACTIVE";

        var psi = new ProcessStartInfo("schtasks.exe", args)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var proc = Process.Start(psi);
        if (proc != null)
            await proc.WaitForExitAsync();
    }

    /// <summary>
    /// 移除定时扫描任务
    /// </summary>
    public async Task UnregisterScheduledTaskAsync()
    {
        var psi = new ProcessStartInfo("schtasks.exe", $"/delete /tn \"{TaskName}\" /f")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var proc = Process.Start(psi);
        if (proc != null)
            await proc.WaitForExitAsync();
    }

    /// <summary>
    /// 检查是否已注册定时任务
    /// </summary>
    public async Task<bool> IsTaskRegisteredAsync()
    {
        var psi = new ProcessStartInfo("schtasks.exe", $"/query /tn \"{TaskName}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var proc = Process.Start(psi);
        if (proc == null) return false;

        await proc.WaitForExitAsync();
        return proc.ExitCode == 0;
    }

    /// <summary>
    /// 获取定时任务下次运行时间
    /// </summary>
    public async Task<DateTime?> GetNextRunTimeAsync()
    {
        var psi = new ProcessStartInfo("schtasks.exe", $"/query /tn \"{TaskName}\" /fo CSV /nh")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var proc = Process.Start(psi);
        if (proc == null) return null;

        string output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0) return null;

        // 解析 schtasks CSV 输出：任务名,下次运行时间,状态
        try
        {
            var parts = output.Split(',');
            if (parts.Length >= 2)
            {
                string nextRun = parts[1].Trim('"', ' ');
                if (DateTime.TryParse(nextRun, out var dt))
                    return dt;
            }
        }
        catch { }

        return null;
    }
}

using System.Security.Principal;

namespace DupFinder.Helpers;

/// <summary>
/// 管理员权限检测与提升工具类
/// </summary>
public static class AdminHelper
{
    /// <summary>
    /// 检测当前进程是否以管理员权限运行
    /// </summary>
    public static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// 以管理员权限重新启动当前应用程序
    /// </summary>
    public static void RestartAsAdmin()
    {
        var exePath = Environment.ProcessPath;
        if (exePath == null) return;

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            System.Diagnostics.Process.Start(startInfo);
            System.Environment.Exit(0);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // 用户拒绝了 UAC 提权，忽略异常
        }
    }
}

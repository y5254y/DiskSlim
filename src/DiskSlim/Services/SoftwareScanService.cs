using DiskSlim.Models;
using Microsoft.Win32;

namespace DiskSlim.Services;

/// <summary>
/// 已安装软件扫描服务实现，从注册表 Uninstall 键读取已安装软件信息
/// 注册表路径：HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall
///             HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall（用户级安装）
///             HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall（32位软件）
/// </summary>
public class SoftwareScanService : ISoftwareScanService
{
    private static readonly string[] UninstallPaths = new[]
    {
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    };

    /// <summary>
    /// 异步扫描所有已安装软件
    /// </summary>
    public async Task<IReadOnlyList<SoftwareInfo>> ScanInstalledSoftwareAsync(
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var result = new List<SoftwareInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 扫描 HKLM（系统级安装）
            ScanRegistryHive(Registry.LocalMachine, result, seen);

            // 扫描 HKCU（当前用户安装）
            ScanRegistryHive(Registry.CurrentUser, result, seen);

            // 按占用空间降序排列
            result.Sort((a, b) => b.InstallSizeBytes.CompareTo(a.InstallSizeBytes));

            return (IReadOnlyList<SoftwareInfo>)result;
        }, cancellationToken);
    }

    /// <summary>
    /// 筛选出安装在系统盘的软件
    /// </summary>
    public IReadOnlyList<SoftwareInfo> FilterSystemDriveSoftware(IEnumerable<SoftwareInfo> allSoftware)
    {
        return allSoftware.Where(s => s.IsOnSystemDrive).ToList();
    }

    // ===== 私有方法 =====

    /// <summary>扫描指定注册表根键下的所有软件</summary>
    private static void ScanRegistryHive(
        RegistryKey hive,
        List<SoftwareInfo> result,
        HashSet<string> seen)
    {
        foreach (string uninstallPath in UninstallPaths)
        {
            try
            {
                using var uninstallKey = hive.OpenSubKey(uninstallPath);
                if (uninstallKey == null) continue;

                foreach (string subKeyName in uninstallKey.GetSubKeyNames())
                {
                    try
                    {
                        using var subKey = uninstallKey.OpenSubKey(subKeyName);
                        if (subKey == null) continue;

                        var info = ReadSoftwareInfo(subKey, $@"{hive.Name}\{uninstallPath}\{subKeyName}");
                        if (info == null) continue;

                        // 去重（按名称+版本）
                        string dedupeKey = $"{info.DisplayName}|{info.Version}";
                        if (seen.Add(dedupeKey))
                        {
                            result.Add(info);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    /// <summary>从注册表子键读取软件信息</summary>
    private static SoftwareInfo? ReadSoftwareInfo(RegistryKey key, string registryPath)
    {
        string? displayName = key.GetValue("DisplayName")?.ToString();
        if (string.IsNullOrWhiteSpace(displayName)) return null;

        // 过滤系统组件和更新包
        string? systemComponent = key.GetValue("SystemComponent")?.ToString();
        if (systemComponent == "1") return null;

        string? releaseType = key.GetValue("ReleaseType")?.ToString();
        if (releaseType is "Update" or "Hotfix") return null;

        // 读取安装大小（注册表中以 KB 为单位）
        long sizeBytes = -1;
        if (key.GetValue("EstimatedSize") is int sizeKb && sizeKb > 0)
            sizeBytes = (long)sizeKb * 1024;

        // 解析安装日期（格式 YYYYMMDD）
        DateTime? installDate = null;
        string? dateStr = key.GetValue("InstallDate")?.ToString();
        if (!string.IsNullOrEmpty(dateStr) && dateStr.Length == 8
            && DateTime.TryParseExact(dateStr, "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsedDate))
        {
            installDate = parsedDate;
        }

        string installLocation = key.GetValue("InstallLocation")?.ToString() ?? string.Empty;

        bool installPathExists = !string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation);
        bool isJunction = false;
        string? migratedToPath = null;

        if (installPathExists)
        {
            try
            {
                var attr = File.GetAttributes(installLocation);
                isJunction = (attr & FileAttributes.ReparsePoint) != 0;
                if (isJunction)
                {
                    migratedToPath = new DirectoryInfo(installLocation).LinkTarget ?? "(Junction)";
                }
            }
            catch
            {
                // 忽略路径属性读取失败，保持默认值
            }
        }

        return new SoftwareInfo
        {
            DisplayName = displayName,
            Version = key.GetValue("DisplayVersion")?.ToString() ?? string.Empty,
            Publisher = key.GetValue("Publisher")?.ToString() ?? string.Empty,
            InstallLocation = installLocation,
            InstallSizeBytes = sizeBytes,
            InstallDate = installDate,
            UninstallString = key.GetValue("UninstallString")?.ToString() ?? string.Empty,
            RegistryKey = registryPath,
            CanMigrate = installPathExists && !isJunction,
            MigratedToPath = migratedToPath
        };
    }
}

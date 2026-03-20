using System.Diagnostics;
using System.Text;
using DiskSlim.Helpers;
using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// WSL 磁盘空间回收服务实现，通过 wsl 命令列出发行版并使用 diskpart 压缩 vhdx 文件
/// </summary>
public class WslService : IWslService
{
    // 已知 WSL 发行版对应的 Packages 目录名前缀
    private static readonly string[] KnownPackagePrefixes =
    [
        "CanonicalGroupLimited.Ubuntu",
        "CanonicalGroupLimited.UbuntuonWindows",
        "TheDebianProject.DebianGNULinux",
        "KaliLinux.54290C8133894",
        "WhitewaterFoundryLtd.Co.openSUSE",
        "SUSE.openSUSE",
        "AlmaLinux",
        "OracleLinux",
        "RockyLinux",
        "CanonicalGroupLimited.Ubuntu22.04onWindows",
        "CanonicalGroupLimited.Ubuntu20.04onWindows",
        "CanonicalGroupLimited.Ubuntu18.04onWindows",
    ];

    /// <inheritdoc />
    public async Task<bool> IsWslInstalledAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunProcessAsync("wsl", "--status", cancellationToken);
            return true;
        }
        catch
        {
            // wsl.exe 不存在或未安装
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(
        CancellationToken cancellationToken = default)
    {
        var distributions = new List<WslDistribution>();

        try
        {
            // wsl --list --verbose 输出含 BOM 的 UTF-16，需特殊处理
            var output = await RunWslListAsync(cancellationToken);
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines.Skip(1)) // 跳过标题行
            {
                var cleaned = line.Trim().TrimStart('*').Trim();
                if (string.IsNullOrWhiteSpace(cleaned)) continue;

                var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 1) continue;

                var name = parts[0];
                var isRunning = parts.Length >= 2 &&
                    parts[1].Equals("Running", StringComparison.OrdinalIgnoreCase);

                var dist = new WslDistribution
                {
                    Name = name,
                    IsRunning = isRunning,
                };

                // 尝试找到对应的 vhdx 文件
                FindVhdxPath(dist);

                distributions.Add(dist);
            }
        }
        catch
        {
            // WSL 未安装或无发行版，返回空列表
        }

        return distributions;
    }

    /// <inheritdoc />
    public async Task<WslReclaimResult> ReclaimDiskSpaceAsync(
        WslDistribution distribution,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        long sizeBefore = GetFileSize(distribution.VhdxPath);

        try
        {
            if (!distribution.VhdxFound)
            {
                return new WslReclaimResult(
                    IsSuccess: false,
                    DistributionName: distribution.Name,
                    SizeBeforeBytes: 0,
                    SizeAfterBytes: 0,
                    Output: string.Empty,
                    ErrorMessage: $"未找到 {distribution.Name} 的 vhdx 文件");
            }

            // 第一步：关闭 WSL（确保 vhdx 未被锁定）
            progress?.Report($"[{distribution.Name}] 正在关闭 WSL…");
            await RunProcessAsync("wsl", "--shutdown", cancellationToken);
            await Task.Delay(2000, cancellationToken); // 等待 WSL 完全关闭

            // 第二步：创建临时 diskpart 脚本并压缩 vhdx
            progress?.Report($"[{distribution.Name}] 正在通过 diskpart 压缩虚拟磁盘…");
            var (diskpartOutput, diskpartError) = await RunDiskpartCompactAsync(
                distribution.VhdxPath, cancellationToken);

            long sizeAfter = GetFileSize(distribution.VhdxPath);
            var fullOutput = new StringBuilder();
            fullOutput.AppendLine("=== WSL 关闭完成 ===");
            fullOutput.AppendLine("=== diskpart 压缩输出 ===");
            fullOutput.Append(diskpartOutput);
            if (!string.IsNullOrWhiteSpace(diskpartError))
            {
                fullOutput.AppendLine("=== 错误输出 ===");
                fullOutput.Append(diskpartError);
            }

            progress?.Report($"[{distribution.Name}] 完成！节省 {FileSizeHelper.Format(sizeBefore - sizeAfter)}");

            return new WslReclaimResult(
                IsSuccess: true,
                DistributionName: distribution.Name,
                SizeBeforeBytes: sizeBefore,
                SizeAfterBytes: sizeAfter,
                Output: fullOutput.ToString());
        }
        catch (Exception ex)
        {
            return new WslReclaimResult(
                IsSuccess: false,
                DistributionName: distribution.Name,
                SizeBeforeBytes: sizeBefore,
                SizeAfterBytes: GetFileSize(distribution.VhdxPath),
                Output: string.Empty,
                ErrorMessage: ex.Message);
        }
    }

    /// <summary>
    /// 使用 diskpart 压缩指定 vhdx 文件，返回输出和错误文本
    /// </summary>
    private static async Task<(string Output, string Error)> RunDiskpartCompactAsync(
        string vhdxPath, CancellationToken cancellationToken)
    {
        // 验证路径中不含会破坏 diskpart 脚本语法的特殊字符
        if (vhdxPath.Contains('"'))
            throw new InvalidOperationException($"vhdx 路径包含不支持的字符（引号）：{vhdxPath}");

        // 创建临时 diskpart 脚本文件
        var scriptPath = Path.Combine(Path.GetTempPath(), $"diskslim_wsl_{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(scriptPath,
                $"select vdisk file=\"{vhdxPath}\"\r\nattach vdisk readonly\r\ncompact vdisk\r\ndetach vdisk\r\nexit\r\n",
                cancellationToken);

            var psi = new ProcessStartInfo("diskpart.exe", $"/s \"{scriptPath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("无法启动 diskpart.exe");

            string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return (output, error);
        }
        finally
        {
            // 清理临时脚本文件；删除失败不影响主流程（如文件已被系统清理）
            try { File.Delete(scriptPath); } catch { }
        }
    }

    /// <summary>
    /// 运行 wsl --list --verbose 并处理 UTF-16 BOM 编码
    /// </summary>
    private static async Task<string> RunWslListAsync(CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo("wsl", "--list --verbose")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.Unicode  // WSL 输出为 UTF-16 LE
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("无法启动 wsl.exe");

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output;
    }

    /// <summary>
    /// 以异步方式运行指定进程并返回标准输出
    /// </summary>
    private static async Task<string> RunProcessAsync(
        string fileName, string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"无法启动 {fileName}");

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output;
    }

    /// <summary>
    /// 在已知 Packages 目录中查找发行版对应的 ext4.vhdx 文件路径
    /// </summary>
    private static void FindVhdxPath(WslDistribution dist)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var packagesDir = Path.Combine(localAppData, "Packages");

        if (!Directory.Exists(packagesDir)) return;

        try
        {
            // 先按发行版名称模糊匹配包目录
            var matchDirs = Directory.GetDirectories(packagesDir)
                .Where(d =>
                {
                    var dirName = Path.GetFileName(d);
                    return KnownPackagePrefixes.Any(prefix =>
                        dirName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ||
                        dirName.Contains(dist.Name, StringComparison.OrdinalIgnoreCase);
                });

            foreach (var dir in matchDirs)
            {
                var vhdxPath = Path.Combine(dir, "LocalState", "ext4.vhdx");
                if (File.Exists(vhdxPath))
                {
                    dist.VhdxPath = vhdxPath;
                    dist.VhdxSizeBytes = GetFileSize(vhdxPath);
                    return;
                }
            }

            // 回退：全局搜索（仅在 Packages 一级子目录下查找）
            foreach (var dir in Directory.GetDirectories(packagesDir))
            {
                var vhdxPath = Path.Combine(dir, "LocalState", "ext4.vhdx");
                if (File.Exists(vhdxPath))
                {
                    // 将此 vhdx 关联到第一个名称匹配的发行版
                    var dirName = Path.GetFileName(dir).ToLowerInvariant();
                    if (dirName.Contains(dist.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        dist.VhdxPath = vhdxPath;
                        dist.VhdxSizeBytes = GetFileSize(vhdxPath);
                        return;
                    }
                }
            }
        }
        catch
        {
            // 权限不足或其他错误，忽略
        }
    }

    private static long GetFileSize(string path)
    {
        try { return new FileInfo(path).Length; }
        catch { return 0; }
    }
}

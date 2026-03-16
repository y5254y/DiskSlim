using DiskSlim.Helpers;
using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 清理引擎服务实现，负责系统垃圾扫描和清理操作
/// 支持：系统临时文件、回收站、Windows Update残留、浏览器缓存、日志文件等
/// </summary>
public class CleanupService : ICleanupService
{
    /// <summary>
    /// 获取所有可清理项目列表（含名称、描述、安全等级、图标）
    /// </summary>
    public IReadOnlyList<CleanupItem> GetCleanupItems()
    {
        var items = new List<CleanupItem>
        {
            // === 🟢 安全级别 ===
            new CleanupItem
            {
                Name = "用户临时文件",
                Description = "Windows 在 %TEMP% 中存放的临时文件，程序退出后通常不再需要。可安全删除。",
                Safety = SafetyLevel.Safe,
                IconGlyph = "\uE74D",
                IsSelected = true,
                CleanAction = async (progress, ct) => await CleanTempFolderAsync(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Local\Temp",
                    progress, ct)
            },
            new CleanupItem
            {
                Name = "系统临时文件",
                Description = "Windows 在 C:\\Windows\\Temp 中存放的系统临时文件，可安全删除。",
                Safety = SafetyLevel.Safe,
                IconGlyph = "\uE74D",
                IsSelected = true,
                CleanAction = async (progress, ct) => await CleanTempFolderAsync(
                    @"C:\Windows\Temp", progress, ct)
            },
            new CleanupItem
            {
                Name = "回收站",
                Description = "已删除但尚未永久清除的文件，占用回收站空间。清空后无法通过回收站恢复。",
                Safety = SafetyLevel.Safe,
                IconGlyph = "\uE74F",
                IsSelected = true,
                CleanAction = async (progress, ct) => await CleanRecycleBinAsync(progress, ct)
            },
            new CleanupItem
            {
                Name = "Windows 错误报告",
                Description = "系统崩溃时生成的错误报告文件（.dmp/.wer），通常可安全删除。",
                Safety = SafetyLevel.Safe,
                IconGlyph = "\uE7BA",
                IsSelected = true,
                CleanAction = async (progress, ct) => await CleanFolderAsync(
                    @"C:\ProgramData\Microsoft\Windows\WER", progress, ct)
            },
            new CleanupItem
            {
                Name = "缩略图缓存",
                Description = "Windows 资源管理器为图片/视频生成的预览缩略图缓存，删除后会自动重建，完全安全。",
                Safety = SafetyLevel.Safe,
                IconGlyph = "\uE91B",
                IsSelected = true,
                CleanAction = async (progress, ct) => await CleanThumbnailCacheAsync(progress, ct)
            },
            new CleanupItem
            {
                Name = "日志文件",
                Description = "系统和应用程序产生的 .log 日志文件，可安全删除。",
                Safety = SafetyLevel.Safe,
                IconGlyph = "\uE9F9",
                IsSelected = true,
                CleanAction = async (progress, ct) => await CleanLogFilesAsync(progress, ct)
            },

            // === 🟡 谨慎级别 ===
            new CleanupItem
            {
                Name = "Windows Update 残留",
                Description = "Windows 更新后保留的旧安装包（SoftwareDistribution\\Download），清理后无法回滚更新。建议系统稳定后清理。",
                Safety = SafetyLevel.Caution,
                IconGlyph = "\uE895",
                IsSelected = false,
                CleanAction = async (progress, ct) => await CleanWindowsUpdateCacheAsync(progress, ct)
            },
            new CleanupItem
            {
                Name = "浏览器缓存（Edge）",
                Description = "Microsoft Edge 浏览器的缓存文件，清理后下次访问网站会稍慢（需重新下载缓存）。",
                Safety = SafetyLevel.Caution,
                IconGlyph = "\uE774",
                IsSelected = false,
                CleanAction = async (progress, ct) => await CleanBrowserCacheAsync("Edge", progress, ct)
            },
            new CleanupItem
            {
                Name = "浏览器缓存（Chrome）",
                Description = "Google Chrome 浏览器的缓存文件，清理后下次访问网站会稍慢（需重新下载缓存）。",
                Safety = SafetyLevel.Caution,
                IconGlyph = "\uE774",
                IsSelected = false,
                CleanAction = async (progress, ct) => await CleanBrowserCacheAsync("Chrome", progress, ct)
            },
            new CleanupItem
            {
                Name = "DirectX 着色器缓存",
                Description = "游戏和3D应用生成的着色器编译缓存，删除后游戏首次启动会重新编译（稍慢）。",
                Safety = SafetyLevel.Caution,
                IconGlyph = "\uE7FC",
                IsSelected = false,
                CleanAction = async (progress, ct) => await CleanShaderCacheAsync(progress, ct)
            },

            // === 🔴 危险级别 ===
            new CleanupItem
            {
                Name = "休眠文件 (hiberfil.sys)",
                Description = "支持休眠功能的系统文件，通常占 RAM 容量的 75%~100%（8GB内存 = 最多8GB）。关闭休眠后可永久释放此空间，但电脑将无法使用休眠功能。",
                Safety = SafetyLevel.Danger,
                IconGlyph = "\uE823",
                IsSelected = false,
                CleanAction = async (progress, ct) => await DisableHibernationAsync(progress, ct)
            },
            new CleanupItem
            {
                Name = "虚拟内存（页面文件）",
                Description = "pagefile.sys 是 Windows 虚拟内存文件，通常占 4~16GB。可迁移到其他盘而非删除，直接删除可能导致系统不稳定。",
                Safety = SafetyLevel.Danger,
                IconGlyph = "\uE950",
                IsSelected = false,
                CleanAction = null // 需要在 UI 中引导用户手动迁移
            }
        };

        return items;
    }

    /// <summary>
    /// 异步扫描各清理项的预估可释放大小
    /// </summary>
    public async Task ScanEstimatedSizesAsync(
        IEnumerable<CleanupItem> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            item.IsScanning = true;

            try
            {
                item.EstimatedSize = item.Name switch
                {
                    "用户临时文件" => await GetFolderSizeAsync(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Local\Temp"),
                    "系统临时文件" => await GetFolderSizeAsync(@"C:\Windows\Temp"),
                    "回收站" => await GetRecycleBinSizeAsync(),
                    "Windows 错误报告" => await GetFolderSizeAsync(@"C:\ProgramData\Microsoft\Windows\WER"),
                    "缩略图缓存" => await GetThumbnailCacheSizeAsync(),
                    "日志文件" => await GetLogFilesSizeAsync(),
                    "Windows Update 残留" => await GetFolderSizeAsync(
                        @"C:\Windows\SoftwareDistribution\Download"),
                    "浏览器缓存（Edge）" => await GetBrowserCacheSizeAsync("Edge"),
                    "浏览器缓存（Chrome）" => await GetBrowserCacheSizeAsync("Chrome"),
                    "DirectX 着色器缓存" => await GetShaderCacheSizeAsync(),
                    "休眠文件 (hiberfil.sys)" => GetHibernationFileSize(),
                    "虚拟内存（页面文件）" => GetPageFileSize(),
                    _ => 0
                };
            }
            catch
            {
                item.EstimatedSize = 0;
            }
            finally
            {
                item.IsScanning = false;
            }
        }
    }

    /// <summary>
    /// 执行清理操作，返回实际释放的字节数
    /// </summary>
    public async Task<long> CleanAsync(
        IEnumerable<CleanupItem> items,
        IProgress<CleanupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        long totalCleaned = 0;
        var selectedItems = items.Where(i => i.IsSelected && i.CleanAction != null).ToList();
        int completed = 0;

        foreach (var item in selectedItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new CleanupProgress(item.Name, totalCleaned, completed, selectedItems.Count));

            try
            {
                var itemProgress = new Progress<long>(bytes =>
                {
                    progress?.Report(new CleanupProgress(item.Name, totalCleaned + bytes, completed, selectedItems.Count));
                });

                long freed = await item.CleanAction!(itemProgress, cancellationToken);
                totalCleaned += freed;
                item.IsCleaned = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // 单项清理失败不影响其他项
            }

            completed++;
        }

        return totalCleaned;
    }

    // ===== 私有清理实现方法 =====

    /// <summary>异步清理指定文件夹中的所有文件</summary>
    private static async Task<long> CleanTempFolderAsync(
        string folderPath,
        IProgress<long>? progress,
        CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            long freed = 0;
            if (!Directory.Exists(folderPath)) return 0;

            foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(file);
                    long size = fi.Length;
                    fi.Delete();
                    freed += size;
                    progress?.Report(freed);
                }
                catch { }
            }

            // 清空空文件夹
            foreach (var dir in Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories).Reverse())
            {
                try { Directory.Delete(dir); } catch { }
            }

            return freed;
        }, ct);
    }

    /// <summary>清空文件夹</summary>
    private static async Task<long> CleanFolderAsync(
        string folderPath,
        IProgress<long>? progress,
        CancellationToken ct)
        => await CleanTempFolderAsync(folderPath, progress, ct);

    /// <summary>清空回收站</summary>
    private static async Task<long> CleanRecycleBinAsync(IProgress<long>? progress, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            // 调用 SHQueryRecycleBin 获取回收站大小（简化：直接调用清空并估算为0）
            long size = 0;
            try
            {
                // 调用 Windows API 清空回收站
                SHEmptyRecycleBin(IntPtr.Zero, null, 0x0001 | 0x0002 | 0x0004);
                progress?.Report(size);
            }
            catch { }
            return size;
        }, ct);
    }

    /// <summary>清理缩略图缓存</summary>
    private static async Task<long> CleanThumbnailCacheAsync(IProgress<long>? progress, CancellationToken ct)
    {
        string thumbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\Windows\Explorer");
        return await CleanTempFolderAsync(thumbPath, progress, ct);
    }

    /// <summary>清理日志文件</summary>
    private static async Task<long> CleanLogFilesAsync(IProgress<long>? progress, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            long freed = 0;
            var logPaths = new[]
            {
                @"C:\Windows\Logs",
                @"C:\Windows\INF",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp")
            };

            foreach (var path in logPaths)
            {
                if (!Directory.Exists(path)) continue;
                foreach (var file in Directory.EnumerateFiles(path, "*.log", SearchOption.AllDirectories))
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var fi = new FileInfo(file);
                        long size = fi.Length;
                        fi.Delete();
                        freed += size;
                        progress?.Report(freed);
                    }
                    catch { }
                }
            }

            return freed;
        }, ct);
    }

    /// <summary>清理 Windows Update 下载缓存</summary>
    private static async Task<long> CleanWindowsUpdateCacheAsync(IProgress<long>? progress, CancellationToken ct)
    {
        // 需要先停止 Windows Update 服务
        return await Task.Run(() =>
        {
            long freed = 0;
            string downloadPath = @"C:\Windows\SoftwareDistribution\Download";

            try
            {
                // 停止 wuauserv 服务
                var stopProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "stop wuauserv",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                stopProcess?.WaitForExit(5000);

                freed = CleanTempFolderAsync(downloadPath, progress, ct).GetAwaiter().GetResult();

                // 重新启动服务
                var startProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = "start wuauserv",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                startProcess?.WaitForExit(5000);
            }
            catch { }

            return freed;
        }, ct);
    }

    /// <summary>清理浏览器缓存</summary>
    private static async Task<long> CleanBrowserCacheAsync(string browser, IProgress<long>? progress, CancellationToken ct)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string? cachePath = browser switch
        {
            "Edge" => Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache"),
            "Chrome" => Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"),
            "Firefox" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles"),
            _ => null
        };

        if (cachePath == null || !Directory.Exists(cachePath)) return 0;
        return await CleanTempFolderAsync(cachePath, progress, ct);
    }

    /// <summary>清理 DirectX 着色器缓存</summary>
    private static async Task<long> CleanShaderCacheAsync(IProgress<long>? progress, CancellationToken ct)
    {
        string shaderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"D3DSCache");
        return await CleanTempFolderAsync(shaderPath, progress, ct);
    }

    /// <summary>通过关闭休眠功能释放 hiberfil.sys</summary>
    private static async Task<long> DisableHibernationAsync(IProgress<long>? progress, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            long size = GetHibernationFileSize();
            try
            {
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/hibernate off",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "runas"
                });
                process?.WaitForExit(5000);
                progress?.Report(size);
            }
            catch { size = 0; }
            return size;
        }, ct);
    }

    // ===== 私有大小估算方法 =====

    private static async Task<long> GetFolderSizeAsync(string path)
    {
        if (!Directory.Exists(path)) return 0;
        return await Task.Run(() =>
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { size += new FileInfo(file).Length; } catch { }
                }
            }
            catch { }
            return size;
        });
    }

    private static Task<long> GetRecycleBinSizeAsync()
    {
        // 通过 Shell API 获取回收站大小（简化实现）
        return Task.FromResult(0L);
    }

    private static async Task<long> GetThumbnailCacheSizeAsync()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Microsoft\Windows\Explorer");
        return await GetFolderSizeAsync(path);
    }

    private static async Task<long> GetLogFilesSizeAsync()
    {
        long size = 0;
        size += await GetFolderSizeAsync(@"C:\Windows\Logs");
        return size;
    }

    private static async Task<long> GetBrowserCacheSizeAsync(string browser)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string? cachePath = browser switch
        {
            "Edge" => Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache"),
            "Chrome" => Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"),
            _ => null
        };
        return cachePath != null ? await GetFolderSizeAsync(cachePath) : 0;
    }

    private static async Task<long> GetShaderCacheSizeAsync()
    {
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"D3DSCache");
        return await GetFolderSizeAsync(path);
    }

    private static long GetHibernationFileSize()
    {
        string hibFile = @"C:\hiberfil.sys";
        try { return new FileInfo(hibFile).Length; } catch { return 0; }
    }

    private static long GetPageFileSize()
    {
        string pageFile = @"C:\pagefile.sys";
        try { return new FileInfo(pageFile).Length; } catch { return 0; }
    }

    // Win32 API P/Invoke
    [System.Runtime.InteropServices.DllImport("Shell32.dll")]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);
}

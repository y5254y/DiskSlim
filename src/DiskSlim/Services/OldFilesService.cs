using DiskSlim.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DiskSlim.Services;

/// <summary>
/// 旧文件/临时文件检测服务实现
/// </summary>
public class OldFilesService : IOldFilesService
{
    /// <summary>临时文件扩展名集合</summary>
    private static readonly HashSet<string> TempExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".tmp", ".temp", ".bak", ".old", ".log", ".cache"
    };

    /// <summary>需要跳过的系统目录（避免误删系统文件）</summary>
    private static readonly HashSet<string> SkipDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        @"C:\Windows",
        @"C:\Program Files",
        @"C:\Program Files (x86)",
        @"C:\ProgramData\Microsoft",
        @"C:\System Volume Information"
    };

    /// <summary>
    /// 扫描长期未访问的旧文件
    /// </summary>
    public async Task<IReadOnlyList<OldFileItem>> ScanOldFilesAsync(
        string rootPath,
        int notAccessedDays,
        IProgress<string>? progress,
        CancellationToken token)
    {
        return await Task.Run(() =>
        {
            var results = new List<OldFileItem>();
            var cutoffDate = DateTime.Now.AddDays(-notAccessedDays);

            try
            {
                foreach (var file in SafeEnumerateFiles(rootPath, token))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastAccessTime < cutoffDate && info.Length > 0)
                        {
                            progress?.Report($"发现旧文件：{info.Name}");
                            results.Add(new OldFileItem
                            {
                                FullPath = file,
                                SizeBytes = info.Length,
                                LastAccessed = info.LastAccessTime,
                                LastModified = info.LastWriteTime,
                                FileType = OldFileType.OldFile
                            });
                        }
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }

            // 按未访问天数倒序
            results.Sort((a, b) => a.LastAccessed.CompareTo(b.LastAccessed));
            return (IReadOnlyList<OldFileItem>)results;
        }, token);
    }

    /// <summary>
    /// 扫描临时文件
    /// </summary>
    public async Task<IReadOnlyList<OldFileItem>> ScanTempFilesAsync(
        string rootPath,
        IProgress<string>? progress,
        CancellationToken token)
    {
        return await Task.Run(() =>
        {
            var results = new List<OldFileItem>();

            try
            {
                foreach (var file in SafeEnumerateFiles(rootPath, token))
                {
                    try
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (TempExtensions.Contains(ext))
                        {
                            var info = new FileInfo(file);
                            progress?.Report($"发现临时文件：{info.Name}");
                            results.Add(new OldFileItem
                            {
                                FullPath = file,
                                SizeBytes = info.Length,
                                LastAccessed = info.LastAccessTime,
                                LastModified = info.LastWriteTime,
                                FileType = OldFileType.TempFile
                            });
                        }
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }

            // 按大小倒序
            results.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
            return (IReadOnlyList<OldFileItem>)results;
        }, token);
    }

    /// <summary>
    /// 扫描空文件和 .dmp 崩溃转储文件
    /// </summary>
    public async Task<IReadOnlyList<OldFileItem>> ScanSpecialFilesAsync(
        string rootPath,
        IProgress<string>? progress,
        CancellationToken token)
    {
        return await Task.Run(() =>
        {
            var results = new List<OldFileItem>();

            try
            {
                foreach (var file in SafeEnumerateFiles(rootPath, token))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        string ext = info.Extension.ToLowerInvariant();

                        if (info.Length == 0)
                        {
                            progress?.Report($"发现空文件：{info.Name}");
                            results.Add(new OldFileItem
                            {
                                FullPath = file,
                                SizeBytes = 0,
                                LastAccessed = info.LastAccessTime,
                                LastModified = info.LastWriteTime,
                                FileType = OldFileType.EmptyFile
                            });
                        }
                        else if (ext == ".dmp")
                        {
                            progress?.Report($"发现转储文件：{info.Name}");
                            results.Add(new OldFileItem
                            {
                                FullPath = file,
                                SizeBytes = info.Length,
                                LastAccessed = info.LastAccessTime,
                                LastModified = info.LastWriteTime,
                                FileType = OldFileType.DumpFile
                            });
                        }
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }

            results.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
            return (IReadOnlyList<OldFileItem>)results;
        }, token);
    }

    /// <summary>
    /// 将文件移动到回收站（调用 Shell API）
    /// </summary>
    public Task DeleteToRecycleBinAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                // 使用 Shell32 FileOperation 移动到回收站
                var fileOp = new SHFILEOPSTRUCT
                {
                    wFunc = FO_DELETE,
                    pFrom = filePath + "\0\0",
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
                };
                int result = SHFileOperation(ref fileOp);
                if (result != 0)
                    throw new IOException($"SHFileOperation 失败，错误码：{result}");
            }
            catch (IOException) { throw; }
            catch { }
        });
    }

    /// <summary>
    /// 批量将文件移动到回收站
    /// </summary>
    public async Task<int> BatchDeleteToRecycleBinAsync(IEnumerable<string> filePaths, IProgress<string>? progress)
    {
        int count = 0;
        foreach (var path in filePaths)
        {
            try
            {
                progress?.Report(Path.GetFileName(path));
                await DeleteToRecycleBinAsync(path);
                count++;
            }
            catch { }
        }
        return count;
    }

    /// <summary>
    /// 在资源管理器中打开文件所在目录
    /// </summary>
    public void OpenInExplorer(string filePath)
    {
        try
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (dir != null)
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch { }
    }

    /// <summary>
    /// 安全地枚举文件，跳过受保护的目录
    /// </summary>
    private static IEnumerable<string> SafeEnumerateFiles(string root, CancellationToken token)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            string dir = stack.Pop();

            // 跳过系统保护目录
            if (SkipDirectories.Any(skip => dir.StartsWith(skip, StringComparison.OrdinalIgnoreCase)))
                continue;

            string[] files;
            try
            {
                files = Directory.GetFiles(dir);
            }
            catch { continue; }

            foreach (var file in files)
                yield return file;

            string[] subDirs;
            try
            {
                subDirs = Directory.GetDirectories(dir);
            }
            catch { continue; }

            foreach (var sub in subDirs)
                stack.Push(sub);
        }
    }

    // --- Win32 API：Shell 文件操作 ---
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    private const int FO_DELETE = 0x0003;
    private const int FOF_ALLOWUNDO = 0x0040;
    private const int FOF_NOCONFIRMATION = 0x0010;
    private const int FOF_SILENT = 0x0004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public int wFunc;
        public string pFrom;
        public string pTo;
        public short fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }
}

using DupFinder.Helpers;
using DupFinder.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DupFinder.Services;

/// <summary>
/// 重复文件扫描服务实现
/// 算法：先按文件大小分组（O(n) I/O），再对同大小组计算 SHA256 哈希（精准去重）
/// </summary>
public class DuplicateScanService : IDuplicateScanService
{
    // 文件类型过滤扩展名映射
    private static readonly Dictionary<string, HashSet<string>> FileTypeExtensions = new()
    {
        ["图片"] = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".tiff", ".svg"],
        ["视频"] = [".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".m4v", ".ts"],
        ["音乐"] = [".mp3", ".flac", ".aac", ".wav", ".ogg", ".m4a", ".wma"],
        ["文档"] = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".csv"],
        ["压缩包"] = [".zip", ".rar", ".7z", ".tar", ".gz", ".xz", ".bz2"],
        ["程序"] = [".exe", ".msi", ".dll", ".sys"],
    };

    /// <summary>
    /// 扫描指定目录中的重复文件
    /// </summary>
    public async Task<IReadOnlyList<DuplicateGroup>> ScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken token)
    {
        return await Task.Run(() =>
        {
            // ── 第一阶段：枚举所有文件，按大小分组 ──────────────────────
            progress?.Report(new ScanProgress { Phase = Localizer.Get("Svc.Dup.PhaseEnum", "正在枚举文件…") });

            var sizeGroups = new Dictionary<long, List<string>>();
            int enumerated = 0;

            foreach (var folder in options.ScanFolders)
            {
                token.ThrowIfCancellationRequested();
                if (!Directory.Exists(folder)) continue;

                var searchOption = options.IncludeSubdirectories
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                foreach (var filePath in SafeEnumerateFiles(folder, searchOption, token))
                {
                    token.ThrowIfCancellationRequested();

                    try
                    {
                        var info = new FileInfo(filePath);

                        // 跳过隐藏文件
                        if (options.SkipHiddenFiles &&
                            (info.Attributes & FileAttributes.Hidden) != 0)
                            continue;

                        // 跳过系统文件
                        if (options.SkipSystemFiles &&
                            (info.Attributes & FileAttributes.System) != 0)
                            continue;

                        // 最小文件大小过滤
                        if (info.Length < options.MinFileSizeBytes)
                            continue;

                        // 文件类型过滤
                        if (!string.IsNullOrEmpty(options.FileTypeFilter) &&
                            FileTypeExtensions.TryGetValue(options.FileTypeFilter, out var allowed))
                        {
                            var ext = Path.GetExtension(filePath).ToLowerInvariant();
                            if (!allowed.Contains(ext)) continue;
                        }

                        if (!sizeGroups.TryGetValue(info.Length, out var list))
                        {
                            list = new List<string>();
                            sizeGroups[info.Length] = list;
                        }
                        list.Add(filePath);
                        enumerated++;

                        if (enumerated % 500 == 0)
                        {
                            progress?.Report(new ScanProgress
                            {
                                Phase = Localizer.Get("Svc.Dup.PhaseEnum", "正在枚举文件…"),
                                CurrentFile = info.Name,
                                ScannedCount = enumerated
                            });
                        }
                    }
                    catch
                    {
                        // 跳过无法访问的文件（权限不足、路径过长等），继续枚举其他文件
                    }
                }
            }

            // 只保留大小相同且有 2 个以上文件的组（候选重复组）
            var candidates = sizeGroups
                .Where(kvp => kvp.Value.Count >= 2)
                .ToList();

            // ── 第二阶段：对候选文件计算 SHA256 哈希 ──────────────────────
            var results = new List<DuplicateGroup>();
            int hashComputed = 0;
            int totalToHash = candidates.Sum(c => c.Value.Count);

            foreach (var (fileSize, paths) in candidates)
            {
                token.ThrowIfCancellationRequested();

                var hashGroups = new Dictionary<string, List<(string path, DateTime modified)>>(
                    StringComparer.OrdinalIgnoreCase);

                foreach (var path in paths)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        var hash = ComputeSha256(path);
                        var modified = File.GetLastWriteTime(path);

                        if (!hashGroups.TryGetValue(hash, out var group))
                        {
                            group = new List<(string, DateTime)>();
                            hashGroups[hash] = group;
                        }
                        group.Add((path, modified));

                        hashComputed++;
                        progress?.Report(new ScanProgress
                        {
                            Phase = Localizer.Get("Svc.Dup.PhaseHash", "正在计算哈希…"),
                            CurrentFile = Path.GetFileName(path),
                            ScannedCount = hashComputed,
                            TotalCount = totalToHash
                        });
                    }
                    catch
                    {
                        // 跳过无法读取（被锁定、已删除）的文件，继续处理其他文件
                        hashComputed++;
                    }
                }

                // 只保留真正有重复（2+ 文件）的哈希组
                foreach (var (hash, group) in hashGroups)
                {
                    if (group.Count < 2) continue;

                    var dupGroup = new DuplicateGroup
                    {
                        Hash = hash,
                        FileSize = fileSize
                    };

                    // 按最后修改时间降序（最新的排在最前面）
                    foreach (var (path, modified) in group.OrderByDescending(x => x.modified))
                    {
                        dupGroup.Files.Add(new DuplicateFileItem
                        {
                            FullPath = path,
                            SizeBytes = fileSize,
                            LastModified = modified
                        });
                    }

                    // 默认将最新文件标记为"保留"
                    if (dupGroup.Files.Count > 0)
                        dupGroup.Files[0].IsKeeper = true;

                    results.Add(dupGroup);
                }
            }

            // 按可节省空间降序排列
            results.Sort((a, b) => b.WasteBytes.CompareTo(a.WasteBytes));
            return (IReadOnlyList<DuplicateGroup>)results;
        }, token);
    }

    /// <summary>
    /// 将文件移动到回收站
    /// </summary>
    public async Task DeleteToRecycleBinAsync(string filePath)
    {
        await Task.Run(() => MoveToRecycleBin(filePath));
    }

    /// <summary>
    /// 批量将文件移动到回收站
    /// </summary>
    public async Task<int> BatchDeleteToRecycleBinAsync(
        IEnumerable<string> filePaths,
        IProgress<string>? progress)
    {
        return await Task.Run(() =>
        {
            int count = 0;
            foreach (var path in filePaths)
            {
                try
                {
                    progress?.Report(Path.GetFileName(path));
                    MoveToRecycleBin(path);
                    count++;
                }
                catch
                {
                    // 跳过无法移动到回收站的文件（权限不足、文件已被删除等），继续处理其他文件
                }
            }
            return count;
        });
    }

    /// <summary>
    /// 在资源管理器中打开文件所在目录并高亮选中该文件
    /// </summary>
    public void OpenInExplorer(string filePath)
    {
        try
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch { }
    }

    // ─── 私有辅助方法 ────────────────────────────────────────────────

    /// <summary>
    /// 计算文件的 SHA256 哈希（十六进制字符串）
    /// </summary>
    private static string ComputeSha256(string filePath)
    {
        using var sha = SHA256.Create();
        using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536);
        var hashBytes = sha.ComputeHash(stream);
        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// 安全枚举目录中的文件，跳过无权访问的目录
    /// </summary>
    private static IEnumerable<string> SafeEnumerateFiles(
        string root, SearchOption searchOption, CancellationToken token)
    {
        if (searchOption == SearchOption.TopDirectoryOnly)
        {
            return Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly);
        }

        return EnumerateRecursive(root, token);
    }

    private static IEnumerable<string> EnumerateRecursive(string dir, CancellationToken token)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly);
        }
        catch
        {
            yield break;
        }

        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();
            yield return file;
        }

        IEnumerable<string> subdirs;
        try
        {
            subdirs = Directory.EnumerateDirectories(dir);
        }
        catch
        {
            yield break;
        }

        foreach (var subdir in subdirs)
        {
            token.ThrowIfCancellationRequested();
            foreach (var file in EnumerateRecursive(subdir, token))
                yield return file;
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
    }

    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_SILENT = 0x0004;

    /// <summary>
    /// 使用 Shell API 将文件移动到回收站（可撤销）
    /// </summary>
    private static void MoveToRecycleBin(string filePath)
    {
        var op = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = filePath + "\0\0",
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
        };
        SHFileOperation(ref op);
    }
}

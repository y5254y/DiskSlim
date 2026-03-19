using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 旧文件/临时文件检测服务接口
/// </summary>
public interface IOldFilesService
{
    /// <summary>
    /// 扫描长期未访问的旧文件
    /// </summary>
    /// <param name="rootPath">扫描根路径（默认 C:\Users）</param>
    /// <param name="notAccessedDays">多少天未访问视为旧文件（默认90天）</param>
    /// <param name="progress">进度报告</param>
    /// <param name="token">取消令牌</param>
    Task<IReadOnlyList<OldFileItem>> ScanOldFilesAsync(
        string rootPath,
        int notAccessedDays,
        IProgress<string>? progress,
        CancellationToken token);

    /// <summary>
    /// 扫描临时文件（.tmp/.temp/.bak/.old/.log 等）
    /// </summary>
    /// <param name="rootPath">扫描根路径（默认 C:\Users）</param>
    /// <param name="progress">进度报告</param>
    /// <param name="token">取消令牌</param>
    Task<IReadOnlyList<OldFileItem>> ScanTempFilesAsync(
        string rootPath,
        IProgress<string>? progress,
        CancellationToken token);

    /// <summary>
    /// 扫描零字节空文件和 .dmp 崩溃转储文件
    /// </summary>
    /// <param name="rootPath">扫描根路径</param>
    /// <param name="progress">进度报告</param>
    /// <param name="token">取消令牌</param>
    Task<IReadOnlyList<OldFileItem>> ScanSpecialFilesAsync(
        string rootPath,
        IProgress<string>? progress,
        CancellationToken token);

    /// <summary>
    /// 将指定文件移动到回收站
    /// </summary>
    /// <param name="filePath">文件路径</param>
    Task DeleteToRecycleBinAsync(string filePath);

    /// <summary>
    /// 批量将文件移动到回收站
    /// </summary>
    /// <param name="filePaths">文件路径列表</param>
    /// <param name="progress">进度报告（当前正在删除的文件名）</param>
    Task<int> BatchDeleteToRecycleBinAsync(IEnumerable<string> filePaths, IProgress<string>? progress);

    /// <summary>
    /// 在资源管理器中打开文件所在文件夹
    /// </summary>
    /// <param name="filePath">文件路径</param>
    void OpenInExplorer(string filePath);
}

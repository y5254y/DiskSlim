using DupFinder.Models;

namespace DupFinder.Services;

/// <summary>
/// 重复文件扫描服务接口
/// </summary>
public interface IDuplicateScanService
{
    /// <summary>
    /// 扫描指定目录中的重复文件
    /// </summary>
    /// <param name="options">扫描选项（扫描路径、过滤条件等）</param>
    /// <param name="progress">进度报告（当前扫描的文件路径）</param>
    /// <param name="token">取消令牌</param>
    /// <returns>重复文件组列表，按可节省空间降序排列</returns>
    Task<IReadOnlyList<DuplicateGroup>> ScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress,
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
    /// <returns>成功删除的文件数量</returns>
    Task<int> BatchDeleteToRecycleBinAsync(
        IEnumerable<string> filePaths,
        IProgress<string>? progress);

    /// <summary>
    /// 在资源管理器中打开文件所在目录并高亮选中该文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    void OpenInExplorer(string filePath);
}

/// <summary>
/// 扫描进度报告数据
/// </summary>
public class ScanProgress
{
    /// <summary>当前正在处理的文件路径</summary>
    public string CurrentFile { get; set; } = string.Empty;

    /// <summary>当前阶段描述（如"正在枚举文件"、"正在计算哈希"）</summary>
    public string Phase { get; set; } = string.Empty;

    /// <summary>已扫描文件数</summary>
    public int ScannedCount { get; set; }

    /// <summary>总文件数（枚举完成后才有意义）</summary>
    public int TotalCount { get; set; }
}

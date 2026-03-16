using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 符号链接服务接口，用于创建/检测/删除 NTFS Junction 符号链接
/// </summary>
public interface ISymlinkService
{
    /// <summary>
    /// 创建 Junction 类型符号链接（目录链接）
    /// </summary>
    /// <param name="linkPath">链接路径（原始路径，将被替换为链接）</param>
    /// <param name="targetPath">目标路径（实际文件存放的新位置）</param>
    /// <returns>是否创建成功</returns>
    Task<bool> CreateJunctionAsync(string linkPath, string targetPath);

    /// <summary>
    /// 检测指定路径是否已经是符号链接（Junction）
    /// </summary>
    /// <param name="path">要检测的路径</param>
    /// <returns>如果是 Junction 则返回 true</returns>
    bool IsJunction(string path);

    /// <summary>
    /// 获取符号链接的真实目标路径
    /// </summary>
    /// <param name="junctionPath">Junction 路径</param>
    /// <returns>目标路径，如果不是 Junction 则返回 null</returns>
    string? GetJunctionTarget(string junctionPath);

    /// <summary>
    /// 删除符号链接（仅删除链接本身，不删除目标数据）
    /// </summary>
    /// <param name="junctionPath">Junction 路径</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteJunctionAsync(string junctionPath);
}

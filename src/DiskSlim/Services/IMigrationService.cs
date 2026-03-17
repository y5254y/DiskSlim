using DiskSlim.Models;

namespace DiskSlim.Services;

/// <summary>
/// 文件夹迁移服务接口，负责将用户文件夹从C盘迁移到其他磁盘
/// 迁移后通过符号链接保持原路径可用，应用程序无感知
/// </summary>
public interface IMigrationService
{
    /// <summary>
    /// 获取可迁移的用户文件夹列表（桌面/文档/下载/图片/视频/音乐）
    /// </summary>
    IReadOnlyList<UserFolderInfo> GetMigratableFolders();

    /// <summary>
    /// 异步执行文件夹迁移：复制文件 → 创建符号链接 → 修改注册表路径
    /// </summary>
    /// <param name="task">迁移任务</param>
    /// <param name="progress">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExecuteMigrationAsync(
        MigrationTask task,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 回退迁移操作：还原注册表路径、删除符号链接（数据保留在目标盘）
    /// </summary>
    /// <param name="task">要回退的迁移任务</param>
    Task RollbackMigrationAsync(MigrationTask task);

    /// <summary>
    /// 验证目标路径的可用空间是否足够
    /// </summary>
    /// <param name="sourcePath">源路径</param>
    /// <param name="destinationDrive">目标盘符（如 "D:"）</param>
    /// <returns>目标盘剩余空间是否大于源文件夹大小</returns>
    Task<bool> ValidateDestinationSpaceAsync(string sourcePath, string destinationDrive);
}

/// <summary>
/// 用户文件夹信息（可迁移的特殊文件夹），支持属性变更通知以便 UI 实时更新
/// </summary>
public class UserFolderInfo : System.ComponentModel.INotifyPropertyChanged
{
    /// <summary>文件夹显示名称（如"文档"）</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>当前路径</summary>
    public string CurrentPath { get; set; } = string.Empty;

    /// <summary>注册表键名（Shell Folders 中的键）</summary>
    public string RegistryKey { get; set; } = string.Empty;

    /// <summary>是否已迁移（路径不在系统盘或有符号链接）</summary>
    public bool IsMigrated { get; set; }

    /// <summary>文件夹图标字形</summary>
    public string IconGlyph { get; set; } = "\uE8B7";

    private long _sizeBytes;
    /// <summary>当前大小（字节），扫描后填充，变更时通知 UI</summary>
    public long SizeBytes
    {
        get => _sizeBytes;
        set
        {
            if (_sizeBytes != value)
            {
                _sizeBytes = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(SizeBytes)));
            }
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// 迁移进度报告
/// </summary>
public record MigrationProgress(
    string Stage,        // 阶段描述：复制文件/创建链接/更新注册表
    long BytesCopied,
    long TotalBytes,
    string CurrentFile);

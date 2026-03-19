namespace DiskSlim.Services;

/// <summary>
/// 系统通知服务接口，负责发送 Windows Toast 通知
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 发送一条 Toast 通知
    /// </summary>
    /// <param name="title">通知标题</param>
    /// <param name="message">通知正文</param>
    /// <param name="actionTag">点击通知时的动作标识（可选）</param>
    void ShowToast(string title, string message, string? actionTag = null);

    /// <summary>
    /// 发送磁盘空间不足警告通知
    /// </summary>
    /// <param name="freeBytes">当前可用空间（字节）</param>
    void ShowLowDiskSpaceWarning(long freeBytes);
}

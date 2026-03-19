using DiskSlim.Helpers;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace DiskSlim.Services;

/// <summary>
/// Windows Toast 通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private const string AppId = "DiskSlim.C盘瘦身大师";

    /// <summary>
    /// 发送 Toast 通知
    /// </summary>
    public void ShowToast(string title, string message, string? actionTag = null)
    {
        try
        {
            string xml = $"""
                <toast>
                  <visual>
                    <binding template="ToastGeneric">
                      <text>{System.Net.WebUtility.HtmlEncode(title)}</text>
                      <text>{System.Net.WebUtility.HtmlEncode(message)}</text>
                    </binding>
                  </visual>
                  <actions>
                    <action content="打开 DiskSlim" activationType="foreground" arguments="open"/>
                  </actions>
                </toast>
                """;

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var toast = new ToastNotification(doc);
            var notifier = ToastNotificationManager.CreateToastNotifier(AppId);
            notifier.Show(toast);
        }
        catch
        {
            // 通知发送失败时静默处理
        }
    }

    /// <summary>
    /// 发送磁盘空间不足警告通知
    /// </summary>
    public void ShowLowDiskSpaceWarning(long freeBytes)
    {
        string freeText = FileSizeHelper.Format(freeBytes);
        ShowToast(
            "⚠️ C盘空间不足",
            $"C盘剩余空间仅剩 {freeText}，建议立即清理。",
            "open_diskslim");
    }
}

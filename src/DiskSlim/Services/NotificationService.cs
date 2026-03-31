using DiskSlim.Helpers;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace DiskSlim.Services;

/// <summary>
/// Windows Toast 通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private static readonly string AppId = Localizer.Get("Svc.Notification.AppId", "DiskSlim.C盘瘦身大师");

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
                    <action content="{System.Net.WebUtility.HtmlEncode(Localizer.Get("Svc.Notification.OpenAction", "打开 DiskSlim"))}" activationType="foreground" arguments="open"/>
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
            Localizer.Get("Svc.Notification.LowDiskTitle", "⚠️ C盘空间不足"),
            Localizer.Format("Svc.Notification.LowDiskBody", "C盘剩余空间仅剩 {0}，建议立即清理。", freeText),
            "open_diskslim");
    }
}

namespace DiskSlim.Services;

/// <summary>
/// 系统托盘图标服务接口
/// </summary>
public interface ITrayService
{
    /// <summary>初始化并显示托盘图标</summary>
    void Initialize(nint hwnd);

    /// <summary>更新托盘图标提示信息（显示C盘剩余空间）</summary>
    void UpdateTooltip(string tooltip);

    /// <summary>显示托盘气泡通知</summary>
    void ShowBalloonTip(string title, string message);

    /// <summary>移除托盘图标并释放资源</summary>
    void Dispose();

    /// <summary>用户单击托盘图标时触发</summary>
    event EventHandler? TrayIconClicked;
}

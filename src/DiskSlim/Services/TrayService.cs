using System.Runtime.InteropServices;

namespace DiskSlim.Services;

/// <summary>
/// 系统托盘图标服务实现，通过 Win32 Shell_NotifyIcon API 管理托盘图标
/// </summary>
public class TrayService : ITrayService
{
    // 托盘消息 ID（WM_APP 基址 0x8000 + 1，避免与其他 WM_APP 消息冲突）
    private const uint WmApp = 0x8000;
    private const uint WmTrayIcon = WmApp + 1;

    // Shell_NotifyIcon 操作码
    private const uint NimAdd = 0x00000000;
    private const uint NimModify = 0x00000001;
    private const uint NimDelete = 0x00000002;

    // NOTIFYICONDATA 标志位
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint NifInfo = 0x00000010;

    // 气泡通知图标类型
    private const uint NiifInfo = 0x00000001;

    // 标准系统图标
    private const int IdcArrow = 32512;
    private const int OicInformation = 104;

    private nint _hwnd;
    private bool _initialized;

    /// <inheritdoc />
    public event EventHandler? TrayIconClicked;

    /// <inheritdoc />
    public void Initialize(nint hwnd)
    {
        _hwnd = hwnd;

        var iconData = BuildNotifyIconData("DiskSlim - 正在加载…");
        iconData.uFlags = NifMessage | NifIcon | NifTip;

        NativeMethods.Shell_NotifyIcon(NimAdd, ref iconData);
        _initialized = true;
    }

    /// <inheritdoc />
    public void UpdateTooltip(string tooltip)
    {
        if (!_initialized) return;

        var iconData = BuildNotifyIconData(tooltip);
        iconData.uFlags = NifTip | NifIcon;
        NativeMethods.Shell_NotifyIcon(NimModify, ref iconData);
    }

    /// <inheritdoc />
    public void ShowBalloonTip(string title, string message)
    {
        if (!_initialized) return;

        var iconData = BuildNotifyIconData(tooltip: "DiskSlim");
        iconData.uFlags = NifInfo;
        iconData.szInfoTitle = title.Length > 63 ? title[..63] : title;
        iconData.szInfo = message.Length > 255 ? message[..255] : message;
        iconData.dwInfoFlags = NiifInfo;
        iconData.uTimeout = 3000;

        NativeMethods.Shell_NotifyIcon(NimModify, ref iconData);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_initialized) return;

        var iconData = BuildNotifyIconData(string.Empty);
        NativeMethods.Shell_NotifyIcon(NimDelete, ref iconData);
        _initialized = false;
    }

    /// <summary>
    /// 构造填充了公共字段的 NOTIFYICONDATA 结构体
    /// </summary>
    private NativeMethods.NotifyIconData BuildNotifyIconData(string tooltip)
    {
        // 加载应用程序图标（使用系统信息图标作为默认备用）
        nint hIcon = NativeMethods.LoadIcon(nint.Zero, new nint(OicInformation));

        return new NativeMethods.NotifyIconData
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.NotifyIconData>(),
            hWnd = _hwnd,
            uID = 1,
            uCallbackMessage = WmTrayIcon,
            hIcon = hIcon,
            szTip = tooltip.Length > 127 ? tooltip[..127] : tooltip,
            uFlags = NifMessage | NifIcon | NifTip
        };
    }

    /// <summary>
    /// Win32 P/Invoke 声明
    /// </summary>
    private static class NativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData pnid);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern nint LoadIcon(nint hInstance, nint lpIconName);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct NotifyIconData
        {
            public uint cbSize;
            public nint hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public nint hIcon;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;

            public uint dwState;
            public uint dwStateMask;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;

            public uint uTimeout;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;

            public uint dwInfoFlags;
        }
    }
}

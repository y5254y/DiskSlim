using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Services;

namespace DiskSlim.ViewModels;

/// <summary>
/// 设置页面 ViewModel，管理定时扫描和通知阈值配置
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IScheduleService _scheduleService;
    private readonly INotificationService _notificationService;

    // --- 定时扫描设置 ---

    [ObservableProperty]
    private bool _isScheduleEnabled;

    [ObservableProperty]
    private string _selectedSchedule = "每周";

    [ObservableProperty]
    private int _scheduledHour = 2;

    [ObservableProperty]
    private int _scheduledMinute = 0;

    [ObservableProperty]
    private string _nextRunTimeText = "未设置";

    [ObservableProperty]
    private bool _isScheduleLoading;

    // --- 通知设置 ---

    [ObservableProperty]
    private bool _isNotificationEnabled;

    [ObservableProperty]
    private long _notificationThresholdBytes = 10L * 1024 * 1024 * 1024; // 默认 10GB

    [ObservableProperty]
    private string _selectedThreshold = "10 GB";

    // --- 状态 ---

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>可选扫描计划</summary>
    public List<string> ScheduleOptions { get; } = ["每天", "每周", "每月"];

    /// <summary>可选通知阈值</summary>
    public List<string> ThresholdOptions { get; } = ["1 GB", "5 GB", "10 GB", "20 GB"];

    public SettingsViewModel(IScheduleService scheduleService, INotificationService notificationService)
    {
        _scheduleService = scheduleService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// 初始化并加载当前设置
    /// </summary>
    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        IsScheduleLoading = true;
        try
        {
            bool isRegistered = await _scheduleService.IsTaskRegisteredAsync();
            IsScheduleEnabled = isRegistered;

            if (isRegistered)
            {
                var nextRun = await _scheduleService.GetNextRunTimeAsync();
                NextRunTimeText = nextRun.HasValue
                    ? nextRun.Value.ToString("yyyy-MM-dd HH:mm")
                    : "未知";
            }
            else
            {
                NextRunTimeText = "未设置";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载设置失败：{ex.Message}";
        }
        finally
        {
            IsScheduleLoading = false;
        }
    }

    /// <summary>
    /// 保存定时扫描设置
    /// </summary>
    [RelayCommand]
    public async Task SaveScheduleAsync()
    {
        IsScheduleLoading = true;
        StatusMessage = "正在保存定时扫描设置...";

        try
        {
            if (IsScheduleEnabled)
            {
                var schedule = SelectedSchedule switch
                {
                    "每天" => ScanSchedule.Daily,
                    "每周" => ScanSchedule.Weekly,
                    _ => ScanSchedule.Monthly
                };

                var triggerTime = new TimeSpan(ScheduledHour, ScheduledMinute, 0);
                await _scheduleService.RegisterScheduledTaskAsync(schedule, triggerTime);

                var nextRun = await _scheduleService.GetNextRunTimeAsync();
                NextRunTimeText = nextRun.HasValue
                    ? nextRun.Value.ToString("yyyy-MM-dd HH:mm")
                    : "已注册";

                StatusMessage = $"✅ 定时扫描已设置：{SelectedSchedule} {ScheduledHour:D2}:{ScheduledMinute:D2}";
            }
            else
            {
                await _scheduleService.UnregisterScheduledTaskAsync();
                NextRunTimeText = "未设置";
                StatusMessage = "✅ 定时扫描已关闭";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 保存失败：{ex.Message}";
        }
        finally
        {
            IsScheduleLoading = false;
        }
    }

    /// <summary>
    /// 发送测试通知
    /// </summary>
    [RelayCommand]
    private void SendTestNotification()
    {
        try
        {
            _notificationService.ShowToast(
                "DiskSlim 测试通知",
                "定时扫描功能正常运作，通知设置有效。");
            StatusMessage = "✅ 测试通知已发送，请查看系统通知中心";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ 通知发送失败：{ex.Message}";
        }
    }

    /// <summary>
    /// 选择通知阈值时更新字节数
    /// </summary>
    partial void OnSelectedThresholdChanged(string value)
    {
        NotificationThresholdBytes = value switch
        {
            "1 GB" => 1L * 1024 * 1024 * 1024,
            "5 GB" => 5L * 1024 * 1024 * 1024,
            "20 GB" => 20L * 1024 * 1024 * 1024,
            _ => 10L * 1024 * 1024 * 1024 // 默认 10 GB
        };
    }
}

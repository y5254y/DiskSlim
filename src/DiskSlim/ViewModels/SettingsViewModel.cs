using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiskSlim.Services;
using DiskSlim.Helpers;

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
    private string _selectedSchedule = Localizer.Get("Vm.Settings.ScheduleWeekly", "每周");

    [ObservableProperty]
    private int _scheduledHour = 2;

    [ObservableProperty]
    private int _scheduledMinute = 0;

    [ObservableProperty]
    private string _nextRunTimeText = Localizer.Get("Vm.Settings.NotSet", "未设置");

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

    [ObservableProperty]
    private LanguageOption? _selectedLanguage;

    /// <summary>可选扫描计划</summary>
    public List<string> ScheduleOptions { get; } =
    [
        Localizer.Get("Vm.Settings.ScheduleDaily", "每天"),
        Localizer.Get("Vm.Settings.ScheduleWeekly", "每周"),
        Localizer.Get("Vm.Settings.ScheduleMonthly", "每月")
    ];

    /// <summary>可选通知阈值</summary>
    public List<string> ThresholdOptions { get; } = ["1 GB", "5 GB", "10 GB", "20 GB"];

    /// <summary>可选语言（空字符串表示跟随系统）</summary>
    public List<LanguageOption> LanguageOptions { get; } =
    [
        new LanguageOption(string.Empty, Localizer.Get("Vm.Settings.Language.Auto", "跟随系统")),
        new LanguageOption("zh-CN", Localizer.Get("Vm.Settings.Language.ZhCn", "简体中文")),
        new LanguageOption("en-US", Localizer.Get("Vm.Settings.Language.EnUs", "English"))
    ];

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
                    : Localizer.Get("Vm.Settings.Unknown", "未知");
            }
            else
            {
                NextRunTimeText = Localizer.Get("Vm.Settings.NotSet", "未设置");
            }

            var savedLanguage = AppLanguageManager.GetSavedLanguage();
            SelectedLanguage = LanguageOptions.FirstOrDefault(x =>
                string.Equals(x.Code, savedLanguage, StringComparison.OrdinalIgnoreCase))
                ?? LanguageOptions[0];
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.Settings.LoadFailed", "加载设置失败：{0}", ex.Message);
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
        StatusMessage = Localizer.Get("Vm.Settings.Saving", "正在保存定时扫描设置...");

        try
        {
            if (IsScheduleEnabled)
            {
                var schedule = SelectedSchedule switch
                {
                    _ when SelectedSchedule == Localizer.Get("Vm.Settings.ScheduleDaily", "每天") => ScanSchedule.Daily,
                    _ when SelectedSchedule == Localizer.Get("Vm.Settings.ScheduleWeekly", "每周") => ScanSchedule.Weekly,
                    _ => ScanSchedule.Monthly
                };

                var triggerTime = new TimeSpan(ScheduledHour, ScheduledMinute, 0);
                await _scheduleService.RegisterScheduledTaskAsync(schedule, triggerTime);

                var nextRun = await _scheduleService.GetNextRunTimeAsync();
                NextRunTimeText = nextRun.HasValue
                    ? nextRun.Value.ToString("yyyy-MM-dd HH:mm")
                    : Localizer.Get("Vm.Settings.Registered", "已注册");

                StatusMessage = Localizer.Format("Vm.Settings.ScheduleSaved", "✅ 定时扫描已设置：{0} {1:D2}:{2:D2}", SelectedSchedule, ScheduledHour, ScheduledMinute);
            }
            else
            {
                await _scheduleService.UnregisterScheduledTaskAsync();
                NextRunTimeText = Localizer.Get("Vm.Settings.NotSet", "未设置");
                StatusMessage = Localizer.Get("Vm.Settings.ScheduleDisabled", "✅ 定时扫描已关闭");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.Settings.SaveFailed", "❌ 保存失败：{0}", ex.Message);
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
                Localizer.Get("Vm.Settings.TestNotificationTitle", "DiskSlim 测试通知"),
                Localizer.Get("Vm.Settings.TestNotificationBody", "定时扫描功能正常运作，通知设置有效。"));
            StatusMessage = Localizer.Get("Vm.Settings.TestNotificationSent", "✅ 测试通知已发送，请查看系统通知中心");
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.Settings.TestNotificationFailed", "❌ 通知发送失败：{0}", ex.Message);
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

    [RelayCommand]
    private void SaveLanguage()
    {
        try
        {
            var code = SelectedLanguage?.Code ?? string.Empty;
            AppLanguageManager.SaveLanguage(code);
            StatusMessage = Localizer.Get("Vm.Settings.LanguageSaved", "✅ 语言已保存，重启应用后生效");
        }
        catch (Exception ex)
        {
            StatusMessage = Localizer.Format("Vm.Settings.LanguageSaveFailed", "❌ 语言保存失败：{0}", ex.Message);
        }
    }
}

public sealed class LanguageOption
{
    public string Code { get; }
    public string DisplayName { get; }

    public LanguageOption(string code, string displayName)
    {
        Code = code;
        DisplayName = displayName;
    }
}

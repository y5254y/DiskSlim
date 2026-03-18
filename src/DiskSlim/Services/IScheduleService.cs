namespace DiskSlim.Services;

/// <summary>
/// 定时扫描任务服务接口，负责通过 Windows 任务计划程序注册定时扫描
/// </summary>
public interface IScheduleService
{
    /// <summary>
    /// 注册一个定时扫描任务到 Windows 任务计划程序
    /// </summary>
    /// <param name="schedule">扫描计划（Daily/Weekly/Monthly）</param>
    /// <param name="triggerTime">每次触发的时间（时:分）</param>
    Task RegisterScheduledTaskAsync(ScanSchedule schedule, TimeSpan triggerTime);

    /// <summary>
    /// 移除已注册的定时扫描任务
    /// </summary>
    Task UnregisterScheduledTaskAsync();

    /// <summary>
    /// 检查是否已注册定时扫描任务
    /// </summary>
    Task<bool> IsTaskRegisteredAsync();

    /// <summary>
    /// 获取当前已注册任务的下次运行时间
    /// </summary>
    Task<DateTime?> GetNextRunTimeAsync();
}

/// <summary>
/// 定时扫描计划枚举
/// </summary>
public enum ScanSchedule
{
    /// <summary>每天扫描</summary>
    Daily,
    /// <summary>每周扫描</summary>
    Weekly,
    /// <summary>每月扫描</summary>
    Monthly
}

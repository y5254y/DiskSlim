using DiskSlim.Services;
using DiskSlim.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace DiskSlim;

/// <summary>
/// 应用程序入口，负责初始化依赖注入容器和启动主窗口
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 全局依赖注入服务容器
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// 主窗口实例
    /// </summary>
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
        Services = ConfigureServices();
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 注册服务接口与实现（Phase 1 + 2）
        services.AddSingleton<IDiskScanService, DiskScanService>();
        services.AddSingleton<ICleanupService, CleanupService>();
        services.AddSingleton<IMigrationService, MigrationService>();
        services.AddSingleton<ISymlinkService, SymlinkService>();
        services.AddSingleton<ISoftwareScanService, SoftwareScanService>();
        services.AddSingleton<ICleanupReportService, CleanupReportService>();

        // Phase 3 Pro 版新增服务
        services.AddSingleton<ISnapshotService, SnapshotService>();
        services.AddSingleton<IOldFilesService, OldFilesService>();
        services.AddSingleton<IScheduleService, ScheduleService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // 注册 ViewModel（Phase 1 + 2）
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CleanupViewModel>();
        services.AddTransient<MigrationViewModel>();
        services.AddSingleton<SoftwareMoveViewModel>();
        services.AddTransient<CleanupReportViewModel>();

        // Phase 3 Pro 版新增 ViewModel
        services.AddTransient<SnapshotViewModel>();
        services.AddTransient<TrendViewModel>();
        services.AddTransient<OldFilesViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 应用程序启动时调用，创建并显示主窗口
    /// </summary>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 初始化清理报告数据库
        var reportService = Services.GetRequiredService<ICleanupReportService>();
        await reportService.InitializeAsync();

        // 初始化快照数据库（Phase 3）
        var snapshotService = Services.GetRequiredService<ISnapshotService>();
        await snapshotService.InitializeAsync();

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}

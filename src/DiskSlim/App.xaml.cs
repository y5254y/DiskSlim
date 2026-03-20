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

    /// <summary>
    /// 托盘图标刷新定时器（保持引用以防 GC 回收）
    /// </summary>
    private static System.Timers.Timer? _trayTimer;

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

        // Phase 4 新增服务
        services.AddSingleton<ICompactOsService, CompactOsService>();
        services.AddSingleton<IWslService, WslService>();
        services.AddSingleton<ITrayService, TrayService>();

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

        // Phase 4 新增 ViewModel
        services.AddTransient<CompactOsViewModel>();
        services.AddTransient<WslViewModel>();

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

        // 初始化系统托盘图标（Phase 4）
        InitializeTrayService();
    }

    /// <summary>
    /// 初始化托盘图标并启动定时刷新 C 盘剩余空间
    /// </summary>
    private static void InitializeTrayService()
    {
        try
        {
            if (MainWindow is null) return;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
            var trayService = Services.GetRequiredService<ITrayService>();
            trayService.Initialize(hwnd);
            trayService.TrayIconClicked += (_, _) => MainWindow?.Activate();

            // 立即更新一次，然后每 30 秒刷新
            UpdateTrayTooltip(trayService);
            _trayTimer = new System.Timers.Timer(30_000);
            _trayTimer.Elapsed += (_, _) => UpdateTrayTooltip(trayService);
            _trayTimer.AutoReset = true;
            _trayTimer.Start();
        }
        catch
        {
            // 托盘图标初始化失败不影响主程序正常运行（如运行在无图形界面的环境中）
        }
    }

    /// <summary>
    /// 读取 C 盘剩余空间并更新托盘图标提示文字
    /// </summary>
    private static void UpdateTrayTooltip(ITrayService trayService)
    {
        try
        {
            var drive = new System.IO.DriveInfo("C");
            if (!drive.IsReady) return;
            double freeGb = drive.AvailableFreeSpace / 1_073_741_824.0;
            double totalGb = drive.TotalSize / 1_073_741_824.0;
            trayService.UpdateTooltip($"DiskSlim\nC盘剩余：{freeGb:F1} GB / {totalGb:F0} GB");
        }
        catch
        {
            // 忽略读取失败
        }
    }
}

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

        // 注册服务接口与实现
        services.AddSingleton<IDiskScanService, DiskScanService>();
        services.AddSingleton<ICleanupService, CleanupService>();
        services.AddSingleton<IMigrationService, MigrationService>();
        services.AddSingleton<ISymlinkService, SymlinkService>();
        services.AddSingleton<ISoftwareScanService, SoftwareScanService>();
        services.AddSingleton<ICleanupReportService, CleanupReportService>();

        // 注册 ViewModel
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CleanupViewModel>();
        services.AddTransient<MigrationViewModel>();
        services.AddTransient<SoftwareMoveViewModel>();
        services.AddTransient<CleanupReportViewModel>();

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

        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}

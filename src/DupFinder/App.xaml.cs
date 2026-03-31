using DupFinder.Helpers;
using DupFinder.Services;
using DupFinder.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace DupFinder;

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
        AppLanguageManager.ApplySavedLanguage();
        this.InitializeComponent();
        Services = ConfigureServices();
    }

    /// <summary>
    /// 配置依赖注入服务
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 扫描服务
        services.AddSingleton<IDuplicateScanService, DuplicateScanService>();

        // ViewModels
        services.AddTransient<ScanViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 应用程序启动时调用，创建并显示主窗口
    /// </summary>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}

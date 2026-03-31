using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DupFinder.Helpers;

namespace DupFinder.ViewModels;

/// <summary>
/// 设置页面 ViewModel
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    // ─── 语言设置 ────────────────────────────────────────────────────

    [ObservableProperty]
    private string _selectedLanguage = "跟随系统";

    /// <summary>支持的语言选项</summary>
    public List<string> LanguageOptions { get; } = ["跟随系统", "简体中文", "English"];

    // ─── 扫描默认值 ──────────────────────────────────────────────────

    [ObservableProperty]
    private bool _defaultSkipHidden = true;

    [ObservableProperty]
    private bool _defaultSkipSystem = true;

    [ObservableProperty]
    private bool _defaultIncludeSubdirs = true;

    // ─── 关于信息 ────────────────────────────────────────────────────

    public string AppVersion => "1.0.0";
    public string AppName => "重复文件清理";
    public string AppDescription => "扫描并清理磁盘中的重复文件，快速释放磁盘空间";

    public SettingsViewModel()
    {
        // 读取已保存的语言设置
        var saved = AppLanguageManager.GetSavedLanguage();
        SelectedLanguage = saved switch
        {
            "zh-CN" => "简体中文",
            "en-US" => "English",
            _ => "跟随系统"
        };
    }

    /// <summary>
    /// 应用语言设置
    /// </summary>
    [RelayCommand]
    private void ApplyLanguage()
    {
        var code = SelectedLanguage switch
        {
            "简体中文" => "zh-CN",
            "English" => "en-US",
            _ => string.Empty
        };
        AppLanguageManager.SaveLanguage(code);
        AppLanguageManager.ApplyLanguage(code);
    }
}

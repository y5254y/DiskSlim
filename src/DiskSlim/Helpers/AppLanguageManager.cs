using System.Globalization;
using Windows.Globalization;
using Windows.Storage;

namespace DiskSlim.Helpers;

internal static class AppLanguageManager
{
    private const string SettingKey = "AppLanguage";

    public static string GetSavedLanguage()
    {
        try
        {
            var value = ApplicationData.Current.LocalSettings.Values[SettingKey] as string;
            return value ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static void SaveLanguage(string languageCode)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[SettingKey] = languageCode ?? string.Empty;
        }
        catch
        {
        }
    }

    public static void ApplySavedLanguage()
    {
        ApplyLanguage(GetSavedLanguage());
    }

    public static void ApplyLanguage(string? languageCode)
    {
        var code = string.IsNullOrWhiteSpace(languageCode) ? string.Empty : languageCode.Trim();
        ApplicationLanguages.PrimaryLanguageOverride = code;

        if (!string.IsNullOrWhiteSpace(code))
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(code);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch
            {
            }
        }
    }
}

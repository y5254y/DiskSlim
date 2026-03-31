using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Windows.ApplicationModel.Resources;

namespace DiskSlim.Helpers;

internal static class Localizer
{
    public static string Get(string key, string fallback)
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallback;

        try
        {
            var value = new ResourceLoader().GetString(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch (COMException)
        {
            return fallback;
        }
    }

    public static string Format(string key, string fallbackFormat, params object[] args)
    {
        var format = Get(key, fallbackFormat);
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace MyPlayer.classes.util;

public enum ThemeType
{
    Light,
    Dark
}

public static class ThemeManager
{
    public static bool IsSystemDarkMode()
    {
        return false;
    }

    public static void ApplyTheme(Window window, ThemeType themeType)
    {
        bool isDark = themeType == ThemeType.Dark;

        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;

        if (isDark)
        {
            window.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            window.Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));
        }
        else
        {
            window.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            window.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }
    }
}

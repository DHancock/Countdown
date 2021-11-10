using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed class SettingsViewModel : PropertyChangedBase
{

    public static ElementTheme SelectedTheme
    {
        get => Settings.CurrentTheme;

        set
        {
            if (App.MainWindow?.Content is FrameworkElement fe)
            {
                fe.RequestedTheme = value;
                Settings.CurrentTheme = value;
            }
        }
    }


    public static bool IsLightTheme
    {
        get { return SelectedTheme == ElementTheme.Light; }
        set { if (value) SelectedTheme = ElementTheme.Light; }
    }

    public static bool IsDarkTheme
    {
        get { return SelectedTheme == ElementTheme.Dark; }
        set { if (value) SelectedTheme = ElementTheme.Dark; }
    }

    public static bool IsSystemTheme
    {
        get { return SelectedTheme == ElementTheme.Default; }
        set { if (value) SelectedTheme = ElementTheme.Default; }
    }
}

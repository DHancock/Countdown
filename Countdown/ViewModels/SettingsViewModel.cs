using Countdown.Models;
using Countdown.Utils;

namespace Countdown.ViewModels;

internal sealed class SettingsViewModel
{
    private readonly Model model;

    public SettingsViewModel(Model model)
    {
        this.model = model;
    }

    public ElementTheme SelectedTheme
    {
        get => model.Settings.CurrentTheme;

        set
        {
            model.Settings.CurrentTheme = value;
            ThemeHelper.Instance.UpdateTheme(value);
        }
    }


    public bool IsLightTheme
    {
        get { return SelectedTheme == ElementTheme.Light; }
        set { if (value) SelectedTheme = ElementTheme.Light; }
    }

    public bool IsDarkTheme
    {
        get { return SelectedTheme == ElementTheme.Dark; }
        set { if (value) SelectedTheme = ElementTheme.Dark; }
    }

    public bool IsSystemTheme
    {
        get { return SelectedTheme == ElementTheme.Default; }
        set { if (value) SelectedTheme = ElementTheme.Default; }
    }
}

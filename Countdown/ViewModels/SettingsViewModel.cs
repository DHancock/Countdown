using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed class SettingsViewModel : PropertyChangedBase
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
            if (App.MainWindow?.Content is FrameworkElement fe)
            {
                fe.RequestedTheme = value;
                model.Settings.CurrentTheme = value;
            }
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

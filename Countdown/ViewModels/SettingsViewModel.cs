namespace Countdown.ViewModels;

internal sealed partial class SettingsViewModel : PropertyChangedBase
{
    public StopwatchController StopwatchController { get; }

    public SettingsViewModel(StopwatchController sc)
    {
        StopwatchController = sc;
    }

    public ElementTheme SelectedTheme
    {
        get => Settings.Data.CurrentTheme;

        set
        {
            Settings.Data.CurrentTheme = value;
            RaisePropertyChanged();
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

    public int Volume
    {
        get => Settings.Data.VolumePercentage;
        set
        {
            Settings.Data.VolumePercentage = value;
            RaisePropertyChanged();
        }
    }
}

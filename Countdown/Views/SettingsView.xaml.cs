using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class SettingsView : Page
{
    bool firstLoad = true;

    public SettingsViewModel? ViewModel { get; set; }

    public SettingsView()
    {
        this.InitializeComponent();

        VersionTextBlock.Text = string.Format(VersionTextBlock.Text, typeof(App).Assembly.GetName().Version);

#if DEBUG
        if (App.IsPackaged)
            VersionTextBlock.Text += " (P)";
#endif

        Loaded += async (s, e) =>
        {
            if (firstLoad)
            {
                firstLoad = false;

                if (IsDarkTheme)
                    AboutImageDark.Opacity = 1;
                else
                    AboutImageLight.Opacity = 1;

                AboutImageLight.Source = await MainWindow.LoadEmbeddedImageResource("Countdown.Resources.256-light.png");
                AboutImageDark.Source = await MainWindow.LoadEmbeddedImageResource("Countdown.Resources.256-dark.png");

                // set duration for the next transition
                LightFader.Duration = new TimeSpan(0, 0, 0, 0, 125);
                DarkFader.Duration = new TimeSpan(0, 0, 0, 0, 125);
            }

            ActualThemeChanged += SettingsView_ActualThemeChanged;
        };

        Unloaded += (s, e) =>
        {
            ActualThemeChanged -= SettingsView_ActualThemeChanged;
        };
    }

    private void SettingsView_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (IsDarkTheme)
        {
            AboutImageLight.Opacity = 0;
            AboutImageDark.Opacity = 1;
        }
        else
        {
            AboutImageLight.Opacity = 1;
            AboutImageDark.Opacity = 0;
        }
    }

    private bool IsDarkTheme
    {
        get
        {
            if (ActualTheme == ElementTheme.Default)
                return App.Current.RequestedTheme == ApplicationTheme.Dark;

            return ActualTheme == ElementTheme.Dark;
        }
    }
}

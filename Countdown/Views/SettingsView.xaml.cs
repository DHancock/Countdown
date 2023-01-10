using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class SettingsView : Page
{
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
            AboutImage.Source ??= await LoadAboutImage();
        };

        ActualThemeChanged += async (s, e) =>
        {
            AboutImage.Source = await LoadAboutImage();
        };
    }

    private async Task<BitmapImage> LoadAboutImage()
    {
        const string cLightPath = "Countdown.Resources.256-light.png";
        const string cDarkPath = "Countdown.Resources.256-dark.png";

        bool isDark = false;

        if (ActualTheme == ElementTheme.Default)
            isDark = App.Current.RequestedTheme == ApplicationTheme.Dark;
        else if (ActualTheme == ElementTheme.Dark)
            isDark = true;

        return await MainWindow.LoadEmbeddedImageResource(isDark ? cDarkPath : cLightPath);
    }
}

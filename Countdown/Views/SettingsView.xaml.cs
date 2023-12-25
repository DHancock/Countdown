using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class SettingsView : Page
{
    private bool firstLoad = true;
    public SettingsViewModel? ViewModel { get; set; }

    public SettingsView()
    {
        this.InitializeComponent();

        VersionTextBlock.Text = $"Version: {Path.GetFileNameWithoutExtension(typeof(App).Assembly.GetName().Version?.ToString())}";

        Loaded += (s, e) =>
        {
            if (firstLoad)
            {
                firstLoad = false;
                App.MainWindow?.AddDragRegionEventHandlers(this);
            }

            App.MainWindow?.SetWindowDragRegions();
        };
    }
}

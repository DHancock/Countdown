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
        else
            VersionTextBlock.Text += " (D)";
#endif

        Loaded += (s, e) =>
        {
            App.MainWindow?.SetWindowDragRegions();
        };
    }

    private void Expander_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        App.MainWindow?.SetWindowDragRegions();
    }

    private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        App.MainWindow?.SetWindowDragRegions();
    }
}

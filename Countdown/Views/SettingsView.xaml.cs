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

        VersionTextBlock.Text = $"Version: {Path.GetFileNameWithoutExtension(typeof(App).Assembly.GetName().Version?.ToString())}";

        // Use the Tag to identify that this text block contains a hyperlink. Work around for:
        // https://github.com/microsoft/WindowsAppSDK/issues/4722
        HyperlinkTextBlock.Tag = HyperlinkTextBlock;
    }
}

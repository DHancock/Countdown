using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class SettingsView : Page
{
    public SettingsView()
    {
        this.InitializeComponent();
    }

    public SettingsViewModel? ViewModel { get; set; }

}

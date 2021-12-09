using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class MainWindow : SubClassWindow
{
    private readonly ViewModel rootViewModel = new ViewModel();

    private readonly FrameNavigationOptions frameNavigationOptions = new FrameNavigationOptions()
    {
        TransitionInfoOverride = new SuppressNavigationTransitionInfo(),
        IsNavigationStackEnabled = false,
    };


    public MainWindow()
    {
        this.InitializeComponent();

        Title = "Countdown";

        MinWidth = 660;
        MinHeight = 500;

        WindowSize = new Size(MinWidth, MinHeight);

        // Restoring window state or position isn't implemented because saving
        // user settings isn't supported in WindowsAppSDK 1.0.0 when unpackaged 
        CenterInPrimaryDisplay();

        InitializeTheme();

        // SelectionFollowsFocus is disabled to avoid multiple selection changed events
        // see https://github.com/microsoft/microsoft-ui-xaml/issues/5744
        if (RootNavigationView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled)
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
    }


    private void InitializeTheme()
    {
        if (BackgroundPage.RequestedTheme != SettingsViewModel.SelectedTheme)
            BackgroundPage.RequestedTheme = SettingsViewModel.SelectedTheme;
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            Type? type = Type.GetType($"Countdown.Views.{item.Tag}");

            if (type is not null)
                _ = ContentFrame.NavigateToType(type, null, frameNavigationOptions);
        }
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        switch (e.SourcePageType.Name)
        {
            case "NumbersView": ((NumbersView)e.Content).ViewModel = rootViewModel.NumbersViewModel; break;
            case "LettersView": ((LettersView)e.Content).ViewModel = rootViewModel.LettersViewModel; break;
            case "ConundrumView": ((ConundrumView)e.Content).ViewModel = rootViewModel.ConundrumViewModel; break;
            case "StopwatchView": ((StopwatchView)e.Content).ViewModel = rootViewModel.StopwatchViewModel; break;
            case "SettingsView": ((SettingsView)e.Content).ViewModel = rootViewModel.SettingsViewModel; break;
            default:
                throw new InvalidOperationException();
        }
    }
}

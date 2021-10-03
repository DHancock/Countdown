using System;
using Countdown.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.



namespace Countdown.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    internal sealed partial class MainWindow : SubClassWindow
    {
        private readonly ViewModel rootViewModel = new ViewModel();

        private readonly FrameNavigationOptions frameNavigationOptions = new()
        {
            TransitionInfoOverride = new SuppressNavigationTransitionInfo(),
            IsNavigationStackEnabled = false,
        };


        public MainWindow()

        {
            this.InitializeComponent();

            Icon = $"Resources\\app_16.ico";

            MinWidth = 660;
            MinHeight = 500;

            WindowSize = new Size(MinWidth, MinHeight);

            InitializeTheme();

            // SelectionFollowsFocus is disabled to avoid multiple selection changed events
            // see https://github.com/microsoft/microsoft-ui-xaml/issues/5744
            if (RootNavigationView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled)
                RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
        }


        private void InitializeTheme()
        {
            if (RootNavigationView.RequestedTheme != rootViewModel.SettingsViewModel.SelectedTheme)
                RootNavigationView.RequestedTheme = rootViewModel.SettingsViewModel.SelectedTheme;
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
                    throw new ArgumentOutOfRangeException(nameof(e.Content));
            }
        }
    }
}

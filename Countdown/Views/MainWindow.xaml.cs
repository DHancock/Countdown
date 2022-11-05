using Countdown.Utils;
using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class MainWindow : SubClassWindow
{
    private readonly ViewModel rootViewModel;

    private readonly FrameNavigationOptions frameNavigationOptions = new FrameNavigationOptions()
    {
        TransitionInfoOverride = new SuppressNavigationTransitionInfo(),
        IsNavigationStackEnabled = false,
    };


    public MainWindow()
    {
        this.InitializeComponent();

        RootNavigationView.MenuItems.Add(CreateSettingsNavigationViewItem());

        rootViewModel = new ViewModel();

        appWindow.Closing += async (s, a) =>
        {
            Settings.Data.RestoreBounds = RestoreBounds;
            Settings.Data.WindowState = WindowState;
            await Settings.Data.Save();
        };

        Activated += (s, a) =>
        {
            ThemeHelper.Instance.UpdateTheme(rootViewModel.SettingsViewModel.SelectedTheme);
        };

        if (AppWindowTitleBar.IsCustomizationSupported() && appWindow.TitleBar is not null)
        {
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            SetTitleBar(CustomTitleBar);
            ThemeHelper.Instance.Register(LayoutRoot, appWindow.TitleBar);
        }
        else
        {
            SetWindowIconFromAppIcon();
            appWindow.Title = CustomTitle.Text;
            CustomTitleBar.Visibility = Visibility.Collapsed;
            ThemeHelper.Instance.Register(LayoutRoot);
        }

        // SelectionFollowsFocus is disabled to avoid multiple selection changed events
        // see https://github.com/microsoft/microsoft-ui-xaml/issues/5744
        if (RootNavigationView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled)
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];

        if (Settings.Data.IsFirstRun)
        {
            appWindow.MoveAndResize(CenterInPrimaryDisplay());
        }
        else
        {
            appWindow.MoveAndResize(ValidateRestoreBounds(Settings.Data.RestoreBounds));

            if (Settings.Data.WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            else
                WindowState = Settings.Data.WindowState;
        }
    }

    private RectInt32 ValidateRestoreBounds(Rect windowArea)
    {
        if (windowArea == Rect.Empty)
            return CenterInPrimaryDisplay();

        Rect workingArea = GetWorkingAreaOfClosestMonitor(windowArea);
        Point topLeft = new Point(windowArea.X, windowArea.Y);

        if ((topLeft.Y + windowArea.Height) > workingArea.Bottom)
            topLeft.Y = workingArea.Bottom - windowArea.Height;

        if (topLeft.Y < workingArea.Top)
            topLeft.Y = workingArea.Top;

        if ((topLeft.X + windowArea.Width) > workingArea.Right)
            topLeft.X = workingArea.Right - windowArea.Width;

        if (topLeft.X < workingArea.Left)
            topLeft.X = workingArea.Left;

        Size size = new Size(Math.Min(windowArea.Width, workingArea.Width), Math.Min(windowArea.Height, workingArea.Height));

        return ConvertToRectInt32(new Rect(topLeft, size));
    }

    private object CreateSettingsNavigationViewItem()
    {
        return new NavigationViewItem()
        {
            Tag = nameof(SettingsView),
            AccessKey = "S",
            Icon = new AnimatedIcon()
            {
                Source = new AnimatedSettingsVisualSource(),
                FallbackIconSource = new SymbolIconSource()
                {
                    Symbol = Symbol.Setting,
                },
            },
        };
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
            case nameof(NumbersView): ((NumbersView)e.Content).ViewModel = rootViewModel.NumbersViewModel; break;
            case nameof(LettersView): ((LettersView)e.Content).ViewModel = rootViewModel.LettersViewModel; break;
            case nameof(ConundrumView): ((ConundrumView)e.Content).ViewModel = rootViewModel.ConundrumViewModel; break;
            case nameof(StopwatchView): ((StopwatchView)e.Content).ViewModel = rootViewModel.StopwatchViewModel; break;
            case nameof(SettingsView): ((SettingsView)e.Content).ViewModel = rootViewModel.SettingsViewModel; break;
            default:
                throw new InvalidOperationException();
        }
    }
}

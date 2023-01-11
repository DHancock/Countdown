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

        // the default settings button doesn't have an access key, and there's no way to set one
        RootNavigationView.FooterMenuItems.Add(CreateSettingsNavigationViewItem());

        rootViewModel = new ViewModel();

        appWindow.Closing += async (s, a) =>
        {
            Settings.Data.RestoreBounds = RestoreBounds;
            Settings.Data.WindowState = WindowState;
            await Settings.Data.Save();
        };

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            customTitleBar.ParentAppWindow = appWindow;
            customTitleBar.UpdateThemeAndTransparency(Settings.Data.CurrentTheme);
            customTitleBar.Title = App.cDisplayName;           
            Activated += customTitleBar.ParentWindow_Activated;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }
        else
        {
            customTitleBar.Visibility = Visibility.Collapsed;
        }

        // always set the window icon and title, it's used in the task switcher
        SetWindowIconFromAppIcon();
        appWindow.Title = App.cDisplayName;

        // SelectionFollowsFocus is disabled to avoid multiple selection changed events
        // https://github.com/microsoft/microsoft-ui-xaml/issues/5744
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

        LayoutRoot.Loaded += (s, e) =>
        {
            LayoutRoot.RequestedTheme = Settings.Data.CurrentTheme;
            // set duration for the next theme change
            ThemeBrushTransition.Duration = new TimeSpan(0, 0, 0, 0, 250);
        };
    }

    private RectInt32 ValidateRestoreBounds(RectInt32 windowArea)
    {
        if (windowArea == default)
            return CenterInPrimaryDisplay();

        RectInt32 workArea = DisplayArea.GetFromRect(windowArea, DisplayAreaFallback.Nearest).WorkArea;
        PointInt32 position = new PointInt32(windowArea.X, windowArea.Y);

        if ((position.Y + windowArea.Height) > (workArea.Y + workArea.Height))
            position.Y = (workArea.Y + workArea.Height) - windowArea.Height;

        if (position.Y < workArea.Y)
            position.Y = workArea.Y;

        if ((position.X + windowArea.Width) > (workArea.X + workArea.Width))
            position.X = (workArea.X + workArea.Width) - windowArea.Width;

        if (position.X < workArea.X)
            position.X = workArea.X;

        SizeInt32 size = new SizeInt32(Math.Min(windowArea.Width, workArea.Width),
                                        Math.Min(windowArea.Height, workArea.Height));

        return new RectInt32(position.X, position.Y, size.Width, size.Height);
    }

    private object CreateSettingsNavigationViewItem()
    {
        // When defined in xaml, click handlers are required to start the animation.
        // So might as well just define it in code where it works as is.
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

    public static async Task<BitmapImage> LoadEmbeddedImageResource(string resourcePath)
    {
        BitmapImage bitmapImage = new BitmapImage();

        using (Stream? resourceStream = typeof(App).Assembly.GetManifestResourceStream(resourcePath))
        {
            Debug.Assert(resourceStream is not null);

            using (Windows.Storage.Streams.IRandomAccessStream stream = resourceStream.AsRandomAccessStream())
            {
                await bitmapImage.SetSourceAsync(stream);
            }
        }

        return bitmapImage;
    }
}

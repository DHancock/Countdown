using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class MainWindow : Window
{
    private readonly ViewModel rootViewModel = new ViewModel();

    private readonly FrameNavigationOptions frameNavigationOptions = new FrameNavigationOptions()
    {
        TransitionInfoOverride = new SuppressNavigationTransitionInfo(),
        IsNavigationStackEnabled = false,
    };

    public MainWindow(string title) : this()
    {
        this.InitializeComponent();

        AppWindow.Closing += async (s, a) =>
        {
            Settings.Data.RestoreBounds = RestoreBounds;
            Settings.Data.WindowState = WindowState;
            await Settings.Data.Save();
        };

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            customTitleBar.ParentAppWindow = AppWindow;
            customTitleBar.UpdateThemeAndTransparency(Settings.Data.CurrentTheme);
            customTitleBar.Title = title;           
            Activated += customTitleBar.ParentWindow_Activated;
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        }
        else
        {
            customTitleBar.Visibility = Visibility.Collapsed;
        }

        // always set the window icon and title, it's used in the task switcher
        AppWindow.SetIcon("Resources\\app.ico");
        AppWindow.Title = title;

        // SelectionFollowsFocus is disabled to avoid multiple selection changed events
        // https://github.com/microsoft/microsoft-ui-xaml/issues/5744
        if (RootNavigationView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled)
        {
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
        }

        if (Settings.Data.IsFirstRun)
        {
            AppWindow.MoveAndResize(CenterInPrimaryDisplay());
        }
        else
        {
            AppWindow.MoveAndResize(ValidateRestoreBounds(Settings.Data.RestoreBounds));
        }

        if (Settings.Data.WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = Settings.Data.WindowState;
        }

        LayoutRoot.Loaded += (s, e) =>
        {
            // set duration for the next theme change
            ThemeBrushTransition.Duration = TimeSpan.FromMicroseconds(250);
        };
    }

    private RectInt32 ValidateRestoreBounds(RectInt32 windowArea)
    {
        if (windowArea == default)
        {
            return CenterInPrimaryDisplay();
        }

        RectInt32 workArea = DisplayArea.GetFromRect(windowArea, DisplayAreaFallback.Nearest).WorkArea;
        PointInt32 position = new PointInt32(windowArea.X, windowArea.Y);

        if ((position.Y + windowArea.Height) > (workArea.Y + workArea.Height))
        {
            position.Y = (workArea.Y + workArea.Height) - windowArea.Height;
        }

        if (position.Y < workArea.Y)
        {
            position.Y = workArea.Y;
        }

        if ((position.X + windowArea.Width) > (workArea.X + workArea.Width))
        {
            position.X = (workArea.X + workArea.Width) - windowArea.Width;
        }

        if (position.X < workArea.X)
        {
            position.X = workArea.X;
        }

        SizeInt32 size = new SizeInt32(Math.Min(windowArea.Width, workArea.Width),
                                        Math.Min(windowArea.Height, workArea.Height));

        return new RectInt32(position.X, position.Y, size.Width, size.Height);
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            Type? type = Type.GetType($"Countdown.Views.{item.Tag}");

            if (type is not null)
            {
                _ = ContentFrame.NavigateToType(type, null, frameNavigationOptions);
            }
        }
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        Debug.Assert(e.Content is Page);

        if ((e.Content is Page page) && (page.Tag is null))
        {
            page.Tag = new Phase();

            page.Loaded += (s, e) =>
            {
                Page page = (Page)s;
                Phase phase = (Phase)page.Tag;

                if (phase.Current == 0)
                {
                    phase.Current = 1;
                    AddDragRegionEventHandlers(page);
                }

                SetWindowDragRegions();
            };

            switch (page)
            {
                case NumbersView nv: nv.ViewModel = rootViewModel.NumbersViewModel; break;
                case LettersView lv: lv.ViewModel = rootViewModel.LettersViewModel; break;
                case ConundrumView cv: cv.ViewModel = rootViewModel.ConundrumViewModel; break;
                case StopwatchView sv: sv.ViewModel = rootViewModel.StopwatchViewModel; break;
                case SettingsView stv: stv.ViewModel = rootViewModel.SettingsViewModel; break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    private sealed class Phase
    {
        public int Current { get; set; } = 0;
    }

    private void ContentFrame_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetWindowDragRegions();
    }

    private RectInt32 CenterInPrimaryDisplay()
    {
        RectInt32 workArea = DisplayArea.Primary.WorkArea;
        RectInt32 windowArea;

        double scaleFactor = GetScaleFactor();
        windowArea.Width = ConvertToDeviceSize(InitialWidth, scaleFactor);
        windowArea.Height = ConvertToDeviceSize(InitialHeight, scaleFactor);

        windowArea.Width = Math.Min(windowArea.Width, workArea.Width);
        windowArea.Height = Math.Min(windowArea.Height, workArea.Height);

        windowArea.Y = (workArea.Height - windowArea.Height) / 2;
        windowArea.X = (workArea.Width - windowArea.Width) / 2;

        // guarantee title bar is visible, the minimum window size may trump working area
        windowArea.Y = Math.Max(windowArea.Y, workArea.Y);
        windowArea.X = Math.Max(windowArea.X, workArea.X);

        return windowArea;
    }
}

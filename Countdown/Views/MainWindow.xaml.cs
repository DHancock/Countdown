using Countdown.ViewModels;

namespace Countdown.Views;
public enum WindowState { Normal, Minimized, Maximized }

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class MainWindow : Window
{
    private const double MinWidth = 660;
    private const double MinHeight = 500;
    private const double InitialWidth = 660;
    private const double InitialHeight = 500;

    private readonly HWND hWnd;
    private readonly AppWindow appWindow;
    private readonly SUBCLASSPROC subClassDelegate;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;

    private readonly ViewModel rootViewModel;

    private readonly FrameNavigationOptions frameNavigationOptions = new FrameNavigationOptions()
    {
        TransitionInfoOverride = new SuppressNavigationTransitionInfo(),
        IsNavigationStackEnabled = false,
    };


    public MainWindow()
    {
        this.InitializeComponent();

        hWnd = (HWND)WindowNative.GetWindowHandle(this);

        appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hWnd));
        appWindow.Changed += AppWindow_Changed;

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass(hWnd, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

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
        appWindow.SetIcon("Resources\\app.ico");
        appWindow.Title = App.cDisplayName;

        // SelectionFollowsFocus is disabled to avoid multiple selection changed events
        // https://github.com/microsoft/microsoft-ui-xaml/issues/5744
        if (RootNavigationView.SelectionFollowsFocus == NavigationViewSelectionFollowsFocus.Disabled)
            RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];

        if (Settings.Data.IsFirstRun)
            appWindow.MoveAndResize(CenterInPrimaryDisplay());
        else
            appWindow.MoveAndResize(ValidateRestoreBounds(Settings.Data.RestoreBounds));

        if (Settings.Data.WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        else
            WindowState = Settings.Data.WindowState;

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

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (WindowState == WindowState.Normal)
        {
            if (args.DidPositionChange)
                restorePosition = appWindow.Position;

            if (args.DidSizeChange)
                restoreSize = appWindow.Size;
        }
    }

    private LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        if (uMsg == PInvoke.WM_GETMINMAXINFO)
        {
            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            double scaleFactor = GetScaleFactor();
            minMaxInfo.ptMinTrackSize.X = Math.Max(ConvertToDeviceSize(MinWidth, scaleFactor), minMaxInfo.ptMinTrackSize.X);
            minMaxInfo.ptMinTrackSize.Y = Math.Max(ConvertToDeviceSize(MinHeight, scaleFactor), minMaxInfo.ptMinTrackSize.Y);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    public WindowState WindowState
    {
        get
        {
            if (appWindow.Presenter is OverlappedPresenter op)
            {
                switch (op.State)
                {
                    case OverlappedPresenterState.Minimized: return WindowState.Minimized;
                    case OverlappedPresenterState.Maximized: return WindowState.Maximized;
                    case OverlappedPresenterState.Restored: return WindowState.Normal;
                }
            }

            return WindowState.Normal;
        }

        set
        {
            if (appWindow.Presenter is OverlappedPresenter op)
            {
                switch (value)
                {
                    case WindowState.Minimized: op.Minimize(); break;
                    case WindowState.Maximized: op.Maximize(); break;
                    case WindowState.Normal: op.Restore(); break;
                }
            }
        }
    }

    public RectInt32 RestoreBounds
    {
        get => new RectInt32(restorePosition.X, restorePosition.Y, restoreSize.Width, restoreSize.Height);
    }

    private static int ConvertToDeviceSize(double value, double scalefactor) => Convert.ToInt32(Math.Clamp(value * scalefactor, 0, short.MaxValue));

    private double GetScaleFactor()
    {
        // The xaml may not have loaded yet, so Content.XamlRoot.RasterizationScale isn't an option here
        double dpi = PInvoke.GetDpiForWindow(hWnd);
        return dpi / 96.0;
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

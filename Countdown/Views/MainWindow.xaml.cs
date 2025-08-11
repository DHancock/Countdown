using Countdown.Utils;
using Countdown.ViewModels;

namespace Countdown.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class MainWindow : Window
{
    private DateTime lastPointerTimeStamp;
    private readonly ViewModel rootViewModel = new ViewModel();

    private readonly FrameNavigationOptions frameNavigationOptions = new FrameNavigationOptions()
    {
        TransitionInfoOverride = new SuppressNavigationTransitionInfo(),
        IsNavigationStackEnabled = false,
    };

    public MainWindow(string title) : this()
    {
        this.InitializeComponent();

        AppWindow.Closing += (s, a) =>
        {
            AppWindow.Hide();
            Settings.Instance.RestoreBounds = RestoreBounds;
            Settings.Instance.WindowState = WindowState;
            Settings.Instance.Save();
        };

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            customTitleBar.ParentAppWindow = AppWindow;
            customTitleBar.UpdateThemeAndTransparency(Settings.Instance.CurrentTheme);
            customTitleBar.Title = title;
            customTitleBar.WindowIconArea.PointerPressed += WindowIconArea_PointerPressed;
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

        if (Settings.Instance.IsFirstRun)
        {
            AppWindow.MoveAndResize(CenterInPrimaryDisplay());
        }
        else
        {
            AppWindow.MoveAndResize(ValidateRestoreBounds(Settings.Instance.RestoreBounds));
        }

        if (Settings.Instance.WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = Settings.Instance.WindowState;
        }

        LayoutRoot.Loaded += static (s, e) =>
        {
            FixTextBoxContextFlyoutMenuForThemeChange((DependencyObject)s);
        };
    }

    private static void FixTextBoxContextFlyoutMenuForThemeChange(DependencyObject root)
    {
        TextBox? tb = root.FindChild<TextBox>();

        Debug.Assert(tb is not null);
        Debug.Assert(tb.ContextFlyout is not null);

        if ((tb is not null) && (tb.ContextFlyout is not null))
        {
            // The context flyout is the standard cut/copy/paste menu provided by the sdk.
            // This event handler will affect all other TextBox instances, I can  
            // only assume that they're all sharing a single context flyout.
            tb.ContextFlyout.Opening += ContextFlyout_Opening;
        }

        static void ContextFlyout_Opening(object? sender, object e)
        {
            if ((sender is TextCommandBarFlyout tcbf) && (tcbf.Target is TextBox tb))
            {
                foreach (ICommandBarElement icbe in tcbf.SecondaryCommands)
                {
                    if ((icbe is FrameworkElement fe) && (fe.ActualTheme != tb.ActualTheme))
                    {
                        // update the menu item's text colour for theme changes occuring after the context flyout was created
                        // (this will also update each menu item's tool tip colours)
                        fe.RequestedTheme = tb.ActualTheme;
                    }
                }
            }
        }
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
            Type? type = null;

            switch (item.Tag as string) // trimming requires a compile time constant for the type name
            {
                case "NumbersView": type = Type.GetType("Countdown.Views.NumbersView"); break;
                case "LettersView": type = Type.GetType("Countdown.Views.LettersView"); break;
                case "ConundrumView": type = Type.GetType("Countdown.Views.ConundrumView"); break;
                case "StopwatchView": type = Type.GetType("Countdown.Views.StopwatchView"); break;
                case "SettingsView": type = Type.GetType("Countdown.Views.SettingsView"); break;
            }
            
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

        windowArea.Width = ConvertToDeviceSize(cInitialWidth);
        windowArea.Height = ConvertToDeviceSize(cInitialHeight);

        windowArea.Width = Math.Min(windowArea.Width, workArea.Width);
        windowArea.Height = Math.Min(windowArea.Height, workArea.Height);

        windowArea.Y = (workArea.Height - windowArea.Height) / 2;
        windowArea.X = (workArea.Width - windowArea.Width) / 2;

        // guarantee title bar is visible, the minimum window size may trump working area
        windowArea.Y = Math.Max(windowArea.Y, workArea.Y);
        windowArea.X = Math.Max(windowArea.X, workArea.X);

        return windowArea;
    }

    private void WindowIconArea_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        HideSystemMenu();
        ShowSystemMenu(viaKeyboard: true); // open at keyboard location as not to obscure double clicks

        TimeSpan doubleClickTime = TimeSpan.FromMilliseconds(PInvoke.GetDoubleClickTime());
        DateTime utcNow = DateTime.UtcNow;

        if ((utcNow - lastPointerTimeStamp) < doubleClickTime)
        {
            PostCloseMessage();
        }
        else
        {
            lastPointerTimeStamp = utcNow;
        }
    }
}

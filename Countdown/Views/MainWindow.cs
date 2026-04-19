using Countdown.ViewModels;
using Countdown.Utilities;

// causes conflicts with Composition.AnimationDirection
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Countdown.Views;

public enum WindowState { Normal, Minimized, Maximized }

internal sealed partial class MainWindow : Window
{
    private enum SC
    {
        RESTORE = 0xF120,
        SIZE = 0xF000,
        MOVE = 0xF010,
        MINIMIZE = 0xF020,
        MAXIMIZE = 0xF030,
        CLOSE = 0xF060,
    }

    private const double cMinWidth = 660;
    private const double cMinHeight = 500;
    private const double cInitialWidth = 660;
    private const double cInitialHeight = 500;

    private readonly HWND windowHandle;

    private readonly RelayCommand restoreCommand;
    private readonly RelayCommand moveCommand;
    private readonly RelayCommand sizeCommand;
    private readonly RelayCommand minimizeCommand;
    private readonly RelayCommand maximizeCommand;
    private readonly RelayCommand closeWindowCommand;

    private readonly InputNonClientPointerSource inputNonClientPointerSource;
    private readonly DispatcherTimer dispatcherTimer;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;
    private readonly MenuFlyout systemMenu;
    private double scaleFactor;
    private UnhookWindowsHookExSafeHandle? hookSafeHandle;

    private const nuint cSubClassID = 0;
    private readonly GCHandle thisGCHandle;

    private MainWindow()
    {
        this.InitializeComponent();

        windowHandle = (HWND)WindowNative.GetWindowHandle(this);

        thisGCHandle = GCHandle.Alloc(this);

        unsafe
        {
            bool success = PInvoke.SetWindowSubclass(windowHandle, &NewSubWindowProc, cSubClassID, (nuint)GCHandle.ToIntPtr(thisGCHandle));
            Debug.Assert(success);
        }

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

        dispatcherTimer = InitialiseDragRegionTimer();

        scaleFactor = IntialiseScaleFactor();

        OverlappedPresenter op = (OverlappedPresenter)AppWindow.Presenter;
        op.PreferredMinimumWidth = ConvertToPixels(cMinWidth);
        op.PreferredMinimumHeight = ConvertToPixels(cMinHeight);

        restoreCommand = new RelayCommand(o => PostSysCommandMessage(SC.RESTORE), CanRestore);
        moveCommand = new RelayCommand(o => PostSysCommandMessage(SC.MOVE), CanMove);
        sizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.SIZE), CanSize);
        minimizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MINIMIZE), CanMinimize);
        maximizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MAXIMIZE), CanMaximize);
        closeWindowCommand = new RelayCommand(o => PostSysCommandMessage(SC.CLOSE));

        systemMenu = (MenuFlyout)LayoutRoot.Resources["SystemMenu"];

        AppWindow.Changed += AppWindow_Changed;
        
        Closed += (s, e) =>
        {
            dispatcherTimer.Stop();
        };
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (WindowState == WindowState.Normal)
        {
            if (args.DidPositionChange)
            {
                restorePosition = AppWindow.Position;
            }

            if (args.DidSizeChange)
            {
                restoreSize = AppWindow.Size;
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        const int HTCAPTION = 0x0002;

        GCHandle handle = GCHandle.FromIntPtr((nint)dwRefData);

        if (handle.Target is MainWindow window)
        {
            switch (uMsg)
            {
                case PInvoke.WM_DPICHANGED:
                {
                    window.scaleFactor = (wParam & 0xFFFF) / 96.0;

                    OverlappedPresenter op = (OverlappedPresenter)window.AppWindow.Presenter;
                    op.PreferredMinimumWidth = window.ConvertToPixels(cMinWidth);
                    op.PreferredMinimumHeight = window.ConvertToPixels(cMinHeight);
                    break;
                }

                case PInvoke.WM_SYSCOMMAND when (lParam == (int)VirtualKey.Space):
                {
                    window.ShowSystemMenu(viaKeyboard: true);
                    return (LRESULT)0;
                }

                case PInvoke.WM_SYSCOMMAND when wParam == (int)SC.MINIMIZE:
                {
                    // work around for https://github.com/microsoft/microsoft-ui-xaml/issues/11068
                    CloseMenuPopups(window.Content.XamlRoot);
                    break;
                }

                case PInvoke.WM_NCRBUTTONUP when wParam == HTCAPTION:
                {
                    window.ShowSystemMenu(viaKeyboard: false);
                    return (LRESULT)0;
                }

                case PInvoke.WM_NCLBUTTONDOWN:
                {
                    window.HideSystemMenu();
                    break;
                }

                case PInvoke.WM_ENDSESSION:
                {
                    window.SaveStateOnEndSession();
                    return (LRESULT)0;
                }
            }
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private static void CloseMenuPopups(XamlRoot xamlRoot)
    {
        foreach (Popup popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot))
        {
            if (popup.Child is MenuFlyoutPresenter) // avoid content dialogs
            {
                popup.IsOpen = false;
            }
        }
    }

    private void PostSysCommandMessage(SC command)
    {
        bool success = PInvoke.PostMessage(windowHandle, PInvoke.WM_SYSCOMMAND, (WPARAM)(nuint)command, 0);
        Debug.Assert(success);
    }

    private void ShowSystemMenu(bool viaKeyboard)
    {
        System.Drawing.Point p = default;

        if (viaKeyboard || !PInvoke.GetCursorPos(out p) || !PInvoke.ScreenToClient(windowHandle, ref p))
        {
            p.X = 3;
            p.Y = AppWindow.TitleBar.Height;
        }

        systemMenu.ShowAt(null, new Point(p.X / scaleFactor, p.Y / scaleFactor));
    }

    private void HideSystemMenu()
    {
        if (systemMenu.IsOpen)
        {
            systemMenu.Hide();
        }
    }

    private void MenuFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        AccessKeyManager.ExitDisplayMode();

        hookSafeHandle?.Dispose(); // dispose calls UnhookWindowsHookEx() 
        hookSafeHandle = null;
    }

    private void MenuFlyout_Opening(object? sender, object e)
    {
        Debug.Assert(hookSafeHandle is null);

        unsafe
        {
            hookSafeHandle = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_KEYBOARD, &KeyboardHookProc, null, PInvoke.GetCurrentThreadId());
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT KeyboardHookProc(int code, WPARAM wParam, LPARAM lParam)
    {
        MainWindow window = App.MainWindow;

        Debug.Assert(window.systemMenu.IsOpen);

        if (code >= 0)
        {
            VirtualKey key = (VirtualKey)(nuint)wParam;
            bool isKeyDown = (lParam & 0x8000_0000) == 0;

            if (isKeyDown)
            {
                if (IsAcceleratorKeyModifier(key))
                {
                    window.systemMenu.Hide();
                }
                else if (!IsMenuNavigationKey(key))
                {
                    bool found = false;

                    foreach (MenuFlyoutItemBase itemBase in window.systemMenu.Items)
                    {
                        if (itemBase.AccessKey == key.ToString())
                        {
                            window.systemMenu.Hide();
                            found = true;

                            if (itemBase.IsEnabled)
                            {
                                MenuFlyoutItem item = (MenuFlyoutItem)itemBase;
                                item.Command.Execute(item.CommandParameter);
                            }

                            break; // no duplicate access keys
                        }
                    }

                    if (!found)
                    {
                        Utils.PlayExclamation(); // mimics the old win32 menu
                    }
                }
            }
            else if (key == VirtualKey.Menu) // the menu is being opened via Alt+Space
            {
                AccessKeyManager.EnterDisplayMode(window.Content.XamlRoot);
            }
        }

        return PInvoke.CallNextHookEx(null, code, wParam, lParam);
    }

    private static bool IsAcceleratorKeyModifier(VirtualKey key)
    {
        return (key == VirtualKey.Menu) || (key == VirtualKey.Control) || (key == VirtualKey.Shift) || (key == VirtualKey.LeftWindows) || (key == VirtualKey.RightWindows);
    }

    private static bool IsMenuNavigationKey(VirtualKey key)
    {
        return (key == VirtualKey.Enter) || (key == VirtualKey.Escape) || (key == VirtualKey.Up) || (key == VirtualKey.Down) || (key == VirtualKey.Space);
    }

    public void PostCloseMessage() => PostSysCommandMessage(SC.CLOSE);

    private bool CanRestore(object? param)
    {
        return (AppWindow.Presenter is OverlappedPresenter op) && (op.State == OverlappedPresenterState.Maximized);
    }

    private bool CanMove(object? param)
    {
        if (AppWindow.Presenter is OverlappedPresenter op)
        {
            return op.State != OverlappedPresenterState.Maximized;
        }

        return AppWindow.Presenter.Kind == AppWindowPresenterKind.CompactOverlay;
    }

    private bool CanSize(object? param)
    {
        return (AppWindow.Presenter is OverlappedPresenter op) && op.IsResizable && (op.State != OverlappedPresenterState.Maximized);
    }

    private bool CanMinimize(object? param)
    {
        return (AppWindow.Presenter is OverlappedPresenter op) && op.IsMinimizable;
    }

    private bool CanMaximize(object? param)
    {
        return (AppWindow.Presenter is OverlappedPresenter op) && op.IsMaximizable && (op.State != OverlappedPresenterState.Maximized);
    }

    private WindowState WindowState
    {
        get
        {
            if (AppWindow.Presenter is OverlappedPresenter op)
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
            if (AppWindow.Presenter is OverlappedPresenter op)
            {
                switch (value)
                {
                    case WindowState.Minimized: op.Minimize(); break;
                    case WindowState.Maximized: op.Maximize(); break;
                    case WindowState.Normal: op.Restore(); break;
                }
            }
            else
            {
                Debug.Assert(value == WindowState.Normal);
            }
        }
    }

    private RectInt32 RestoreBounds
    {
        get => new RectInt32(restorePosition.X, restorePosition.Y, restoreSize.Width, restoreSize.Height);
    }

    private int ConvertToPixels(double value)
    {
        Debug.Assert(value >= 0.0);
        Debug.Assert(scaleFactor > 0.0);

        return (int)Math.FusedMultiplyAdd(value, scaleFactor, 0.5);
    }

    private double IntialiseScaleFactor()
    {
        double dpi = PInvoke.GetDpiForWindow(windowHandle);
        return dpi / 96.0;
    }

    private void SetWindowDragRegionsInternal()
    {
        const int cNavigationViewPassthroughCount = 6;

        try
        {
            // as there is no clear distinction any more between the title bar region and the client area,
            // just treat the whole window as a title bar, click anywhere on the backdrop to drag the window.
            RectInt32 windowRect = new RectInt32(0, 0, AppWindow.ClientSize.Width, AppWindow.ClientSize.Height);
            inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, [windowRect]);

            Debug.Assert(selectedPage is not null);
            int size = selectedPage.PassthroughCount + cNavigationViewPassthroughCount;

            RectInt32[] rects = new RectInt32[size];

            selectedPage.AddPassthroughContent(rects);
            AddNavigationViewPassthroughContent(rects);

            inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, rects);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void AddNavigationViewPassthroughContent(RectInt32[] rects)
    {
        int index = rects.Length;

        rects[--index] = Utils.GetPassthroughRect(customTitleBar.WindowIconArea);

        foreach (object item in RootNavigationView.MenuItems)
        {
            rects[--index] = Utils.GetPassthroughRect((UIElement)item); // 4
        }

        foreach (object item in RootNavigationView.FooterMenuItems)
        {
            rects[--index] = Utils.GetPassthroughRect((UIElement)item); // 1
        }
    }

    private DispatcherTimer InitialiseDragRegionTimer()
    {
        DispatcherTimer dt = new DispatcherTimer();
        dt.Interval = TimeSpan.FromMilliseconds(50);
        dt.Tick += DispatcherTimer_Tick;
        return dt;
    }

    public void SetWindowDragRegions()
    {
        // defer setting the drag regions while still resizing the window or scrolling
        // it's content. If the timer is already running, this resets the interval.
        dispatcherTimer.Start();
    }

    private void DispatcherTimer_Tick(object? sender, object e)
    {
        dispatcherTimer.Stop();
        SetWindowDragRegionsInternal();
    } 

    private void SaveStateOnEndSession()
    {
        Settings.Instance.RestoreBounds = RestoreBounds;
        Settings.Instance.WindowState = WindowState;
        Settings.Instance.Save();
    }
}

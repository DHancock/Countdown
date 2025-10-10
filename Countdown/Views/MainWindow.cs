using Countdown.ViewModels;
using Countdown.Utilities;

// causes conflicts with Composition.AnimationDirection
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Countdown.Views;

public enum WindowState { Normal, Minimized, Maximized }

internal partial class MainWindow : Window
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

    private readonly IntPtr windowPtr;

    private readonly RelayCommand restoreCommand;
    private readonly RelayCommand moveCommand;
    private readonly RelayCommand sizeCommand;
    private readonly RelayCommand minimizeCommand;
    private readonly RelayCommand maximizeCommand;
    private readonly RelayCommand closeWindowCommand;

    private readonly InputNonClientPointerSource inputNonClientPointerSource;
    private readonly SUBCLASSPROC subClassDelegate;
    private readonly DispatcherTimer dispatcherTimer;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;
    private readonly MenuFlyout systemMenu;
    private int pixelMinWidth;
    private int pixelMinHeight;
    private double scaleFactor;
    private readonly HOOKPROC hookProc;
    private UnhookWindowsHookExSafeHandle? hookSafeHandle;

    private MainWindow()
    {
        this.InitializeComponent();

        windowPtr = WindowNative.GetWindowHandle(this);

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass((HWND)windowPtr, subClassDelegate, 0, 0))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

        dispatcherTimer = InitialiseDragRegionTimer();

        scaleFactor = IntialiseScaleFactor();
        pixelMinWidth = ConvertToPixels(cMinWidth);
        pixelMinHeight = ConvertToPixels(cMinHeight);

        restoreCommand = new RelayCommand(o => PostSysCommandMessage(SC.RESTORE), CanRestore);
        moveCommand = new RelayCommand(o => PostSysCommandMessage(SC.MOVE), CanMove);
        sizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.SIZE), CanSize);
        minimizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MINIMIZE), CanMinimize);
        maximizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MAXIMIZE), CanMaximize);
        closeWindowCommand = new RelayCommand(o => PostSysCommandMessage(SC.CLOSE));

        systemMenu = (MenuFlyout)LayoutRoot.Resources["SystemMenu"];

        hookProc = new HOOKPROC(KeyboardHookProc);

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

    private LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        const int VK_SPACE = 0x0020;
        const int HTCAPTION = 0x0002;

        switch (uMsg)
        {
            case PInvoke.WM_GETMINMAXINFO:
            {
                unsafe
                {
                    MINMAXINFO* mptr = (MINMAXINFO*)lParam.Value;
                    mptr->ptMinTrackSize.X = pixelMinWidth;
                    mptr->ptMinTrackSize.Y = pixelMinHeight;
                }
                break;
            }

            case PInvoke.WM_DPICHANGED:
            {
                scaleFactor = (wParam & 0xFFFF) / 96.0;
                pixelMinWidth = ConvertToPixels(cMinWidth);
                pixelMinHeight = ConvertToPixels(cMinHeight);
                break;
            }

            case PInvoke.WM_SYSCOMMAND when (lParam == VK_SPACE) && (AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen):
            {
                ShowSystemMenu(viaKeyboard: true);
                return (LRESULT)0;
            }

            case PInvoke.WM_NCRBUTTONUP when wParam == HTCAPTION:
            {
                ShowSystemMenu(viaKeyboard: false);
                return (LRESULT)0;
            }

            case PInvoke.WM_NCLBUTTONDOWN:
            {
                HideSystemMenu();
                break;
            }

            case PInvoke.WM_ENDSESSION:
            {
                SaveStateOnEndSession();
                return (LRESULT)0;
            }
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void PostSysCommandMessage(SC command)
    {
        bool success = PInvoke.PostMessage((HWND)windowPtr, PInvoke.WM_SYSCOMMAND, (WPARAM)(nuint)command, 0);
        Debug.Assert(success);
    }

    private void ShowSystemMenu(bool viaKeyboard)
    {
        System.Drawing.Point p = default;

        if (viaKeyboard || !PInvoke.GetCursorPos(out p) || !PInvoke.ScreenToClient((HWND)windowPtr, ref p))
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
        hookSafeHandle = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_KEYBOARD, hookProc, null, PInvoke.GetCurrentThreadId());
    }

    private LRESULT KeyboardHookProc(int code, WPARAM wParam, LPARAM lParam)
    {
        Debug.Assert(systemMenu.IsOpen);

        if (code >= 0)
        {
            VirtualKey key = (VirtualKey)(nuint)wParam;
            bool isKeyDown = (lParam >>> 31) == 0;

            if (isKeyDown)
            {
                if (IsAcceleratorKeyModifier(key))
                {
                    systemMenu.Hide();
                }
                else if ((key != VirtualKey.Escape) && (key != VirtualKey.Enter) && (key != VirtualKey.Up) && (key != VirtualKey.Down))
                {
                    bool found = false;

                    foreach (MenuFlyoutItemBase itemBase in systemMenu.Items)
                    {
                        if (itemBase.AccessKey == key.ToString())
                        {
                            systemMenu.Hide();
                            found = true;

                            if (itemBase.IsEnabled)
                            {
                                MenuFlyoutItem item = (MenuFlyoutItem)itemBase;
                                item.Command.Execute(item.CommandParameter);
                            }
                        }
                    }

                    if (!found)
                    {
                        Utils.PlayExclamation();
                    }
                }
            }
            else if (key == VirtualKey.Menu) // the menu is being opened via Alt+Space
            {
                AccessKeyManager.EnterDisplayMode(Content.XamlRoot);
            }
        }

        return PInvoke.CallNextHookEx(null, code, wParam, lParam);
    }

    private static bool IsAcceleratorKeyModifier(VirtualKey key)
    {
        return (key == VirtualKey.Menu) || (key == VirtualKey.Control) || (key == VirtualKey.Shift) || (key == VirtualKey.LeftWindows) || (key == VirtualKey.RightWindows);
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
        double dpi = PInvoke.GetDpiForWindow((HWND)windowPtr);
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

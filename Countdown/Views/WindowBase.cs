// included here due to name conflicts
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;

using Countdown.ViewModels;

namespace Countdown.Views;

public enum WindowState { Normal, Minimized, Maximized }

internal abstract class WindowBase : Window
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

    public double MinWidth { get; set; }
    public double MinHeight { get; set; }
    public double InitialWidth { get; set; }
    public double InitialHeight { get; set; }
    public IntPtr WindowPtr { get; }
    public RelayCommand RestoreCommand { get; }
    public RelayCommand MoveCommand { get; }
    public RelayCommand SizeCommand { get; }
    public RelayCommand MinimizeCommand { get; }
    public RelayCommand MaximizeCommand { get; }
    public RelayCommand CloseCommand { get; }

    private readonly InputNonClientPointerSource inputNonClientPointerSource;
    private readonly SUBCLASSPROC subClassDelegate;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;
    private MenuFlyout? systemMenu;

    public WindowBase()
    {
        WindowPtr = WindowNative.GetWindowHandle(this);

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass((HWND)WindowPtr, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

        AppWindow.Changed += AppWindow_Changed;

        RestoreCommand = new RelayCommand(o => PostSysCommandMessage(SC.RESTORE), CanRestore);
        MoveCommand = new RelayCommand(o => PostSysCommandMessage(SC.MOVE), CanMove);
        SizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.SIZE), CanSize);
        MinimizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MINIMIZE), CanMinimize);
        MaximizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MAXIMIZE), CanMaximize);
        CloseCommand = new RelayCommand(o => PostSysCommandMessage(SC.CLOSE));
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange)
        {
            if (WindowState == WindowState.Normal)
            {
                restorePosition = AppWindow.Position;
                restoreSize = AppWindow.Size;
            }
        }
        else if (args.DidPresenterChange) // including properties of the current presenter
        {
            if (AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
                HideSystemMenu();
            else
                UpdateSystemMenuItemsEnabledState();
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
                MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                double scaleFactor = GetScaleFactor();
                minMaxInfo.ptMinTrackSize.X = Math.Max(ConvertToDeviceSize(MinWidth, scaleFactor), minMaxInfo.ptMinTrackSize.X);
                minMaxInfo.ptMinTrackSize.Y = Math.Max(ConvertToDeviceSize(MinHeight, scaleFactor), minMaxInfo.ptMinTrackSize.Y);
                Marshal.StructureToPtr(minMaxInfo, lParam, true);
                break;
            }

            case PInvoke.WM_SYSCOMMAND:
            {
                if ((lParam == VK_SPACE) && (AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen))
                {
                    HideSystemMenu();

                    if (ShowSystemMenu(viaKeyboard: true))
                        return (LRESULT)0;
                }

                break;
            }

            case PInvoke.WM_NCRBUTTONUP:
            {
                if (wParam == HTCAPTION)
                {
                    HideSystemMenu();

                    if (ShowSystemMenu(viaKeyboard: false))
                        return (LRESULT)0;
                }

                break;
            }

            case PInvoke.WM_NCLBUTTONDOWN:
            {
                if (wParam == HTCAPTION)
                {
                    HideSystemMenu();
                }

                break;
            }
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void PostSysCommandMessage(SC command)
    {
        bool success = PInvoke.PostMessage((HWND)WindowPtr, PInvoke.WM_SYSCOMMAND, (WPARAM)(nuint)command, 0);
        Debug.Assert(success);
    }

    private bool ShowSystemMenu(bool viaKeyboard)
    {
        if ((systemMenu is null) && (Content is FrameworkElement root) && root.Resources.TryGetValue("SystemMenuFlyout", out object? res))
            systemMenu = res as MenuFlyout;

        if (systemMenu is not null)
        {
            System.Drawing.Point p = default;

            if (viaKeyboard || !PInvoke.GetCursorPos(out p) || !PInvoke.ScreenToClient((HWND)WindowPtr, ref p))
            {
                p.X = 3;
                p.Y = AppWindow.TitleBar.Height;
            }

            double scale = GetScaleFactor();
            systemMenu.ShowAt(null, new Windows.Foundation.Point(p.X / scale, p.Y / scale));
            return true;
        }

        return false;
    }

    private void HideSystemMenu()
    {
        if ((systemMenu is not null) && systemMenu.IsOpen)
            systemMenu.Hide();
    }

    private void UpdateSystemMenuItemsEnabledState()
    {
        RestoreCommand.RaiseCanExecuteChanged();
        MoveCommand.RaiseCanExecuteChanged();
        SizeCommand.RaiseCanExecuteChanged();
        MinimizeCommand.RaiseCanExecuteChanged();
        MaximizeCommand.RaiseCanExecuteChanged();
    }

    public void PostCloseMessage() => PostSysCommandMessage(SC.CLOSE);

    private bool CanRestore(object? param)
    {
        return (AppWindow.Presenter is OverlappedPresenter op) && (op.State == OverlappedPresenterState.Maximized);
    }

    private bool CanMove(object? param)
    {
        if (AppWindow.Presenter is OverlappedPresenter op)
            return op.State != OverlappedPresenterState.Maximized;

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

    public WindowState WindowState
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

    public RectInt32 RestoreBounds
    {
        get => new RectInt32(restorePosition.X, restorePosition.Y, restoreSize.Width, restoreSize.Height);
    }

    public static int ConvertToDeviceSize(double value, double scaleFactor) => Convert.ToInt32(Math.Clamp(value * scaleFactor, 0, short.MaxValue));

    public double GetScaleFactor()
    {
        if ((Content is not null) && (Content.XamlRoot is not null))
            return Content.XamlRoot.RasterizationScale;

        double dpi = PInvoke.GetDpiForWindow((HWND)WindowPtr);
        return dpi / 96.0;
    }

    public void ClearWindowDragRegions()
    {
        // allow mouse interaction with menu fly outs,  
        // including clicks anywhere in the client area used to dismiss the menu
        if (AppWindowTitleBar.IsCustomizationSupported())
            inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.Caption);
    }

    public void SetWindowDragRegions()
    {
        try
        {
            if ((Content is FrameworkElement layoutRoot) && layoutRoot.IsLoaded && AppWindowTitleBar.IsCustomizationSupported())
            {
                RectInt32 windowRect = new RectInt32(0, 0, AppWindow.ClientSize.Width, AppWindow.ClientSize.Height);
                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, new[] { windowRect });

                List<RectInt32> rects = new List<RectInt32>(27);
                LocatePassThroughContent(rects, layoutRoot);
                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, rects.ToArray());
            }
        }
        catch (Exception ex)
        {
            // accessing Window.Content can throw an object closed exception when
            // a menu unloaded event fires because the window is closing
            Debug.WriteLine(ex);
        }
    }

    private record class ScrollViewerBounds(in Point Offset, in Vector2 Size)
    {
        public double Top => Offset.Y;
    }

    private static void LocatePassThroughContent(List<RectInt32> rects, DependencyObject reference, ScrollViewerBounds? bounds = null)
    {
        static Point GetOffsetFromXamlRoot(UIElement e)
        {
            GeneralTransform gt = e.TransformToVisual(e.XamlRoot.Content);
            return gt.TransformPoint(new Point(0, 0));
        }

        static bool IsValidUIElement(UIElement e)
        {
            return (e.Visibility == Visibility.Visible) && (e.ActualSize.X > 0) && (e.ActualSize.Y > 0) && (e.Opacity > 0);
        }

        if ((reference is UIElement element) && IsValidUIElement(element))
        {
            switch (element)
            {
                case CountdownTextBox:
                case Button:
                case ListView:
                case SplitButton:
                case NavigationViewItem:
                case Expander:
                case ScrollBar:
                case AutoSuggestBox:
                case TextBlock tb when (tb.Inlines.FirstOrDefault(x => x is Hyperlink) is not null):
                {
                    Point offset = GetOffsetFromXamlRoot(element);
                    Vector2 actualSize = element.ActualSize;

                    if ((bounds is not null) && (offset.Y < bounds.Top)) // top clip (for vertical scroll bars)
                    {
                        actualSize.Y -= (float)(bounds.Top - offset.Y);
                        offset.Y = bounds.Top;
                    }

                    rects.Add(ScaledRect(offset, actualSize, element.XamlRoot.RasterizationScale));
                    return;
                }

                case ScrollViewer:
                {
                    // chained scroll viewers is not supported
                    bounds = new ScrollViewerBounds(GetOffsetFromXamlRoot(element), element.ActualSize);
                    break;
                }

                default: break;
            }

            int count = VisualTreeHelper.GetChildrenCount(reference);

            for (int index = 0; index < count; index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(reference, index);
                LocatePassThroughContent(rects, child, bounds);
            }
        }
    }

    private static RectInt32 ScaledRect(in Point location, in Vector2 size, double scale)
    {
        return new RectInt32(Convert.ToInt32(location.X * scale),
                             Convert.ToInt32(location.Y * scale),
                             Convert.ToInt32(size.X * scale),
                             Convert.ToInt32(size.Y * scale));
    }

    public void AddDragRegionEventHandlers(Page page) => AddDragRegionEventHandlers((DependencyObject)page);

    private void AddDragRegionEventHandlers(DependencyObject reference)
    {
        switch (reference)
        {
            case SplitButton:
            case TreeView:
            case ListView:
            {
                FlyoutBase? flyoutBase = null;

                if (reference is TreeView tv)
                    flyoutBase = tv.ContextFlyout;
                else if (reference is ListView lv)
                    flyoutBase = lv.ContextFlyout;
                else if (reference is SplitButton sb)
                    flyoutBase = sb.Flyout;

                if (flyoutBase is not null)
                {
                    flyoutBase.Opened += Flyout_Opened;
                    flyoutBase.Closed += Flyout_Closed;
                }
                return;
            }

            case Expander expander:
            {
                expander.SizeChanged += Expander_SizeChanged;
                return;
            }

            case ScrollViewer scrollViewer:
            {
                scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
                break;
            }

            case Popup popup:  // for the AutoSuggestBox
            {
                popup.Opened += Flyout_Opened;
                popup.Closed += Flyout_Closed;
                return;
            }

            case Button:
            case GroupBox: return;

            default: break;
        }

        int count = VisualTreeHelper.GetChildrenCount(reference);

        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(reference, index);
            AddDragRegionEventHandlers(child);
        }

        void Flyout_Opened(object? sender, object e) => ClearWindowDragRegions();
        void Flyout_Closed(object? sender, object e) => SetWindowDragRegions();
        void Expander_SizeChanged(object sender, SizeChangedEventArgs e) => SetWindowDragRegions();
        void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) => SetWindowDragRegions();
    }
}

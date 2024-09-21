﻿// included here due to name conflicts
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;

using Countdown.ViewModels;
using Countdown.Utils;

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

    private RelayCommand? restoreCommand;
    private RelayCommand? moveCommand;
    private RelayCommand? sizeCommand;
    private RelayCommand? minimizeCommand;
    private RelayCommand? maximizeCommand;
    private RelayCommand? closeCommand;

    private readonly InputNonClientPointerSource inputNonClientPointerSource;
    private readonly SUBCLASSPROC subClassDelegate;
    private readonly DispatcherTimer dispatcherTimer;
    private bool cancelDragRegionTimerEvent = false;
    private PointInt32 restorePosition;
    private SizeInt32 restoreSize;
    private MenuFlyout? systemMenu;
    private int scaledMinWidth;
    private int scaledMinHeight;
    private double scaleFactor;

    private MainWindow()
    {
        windowPtr = WindowNative.GetWindowHandle(this);

        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass((HWND)windowPtr, subClassDelegate, 0, 0))
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);

        dispatcherTimer = InitialiseDragRegionTimer();

        scaleFactor = IntialiseScaleFactor();
        scaledMinWidth = ConvertToDeviceSize(cMinWidth);
        scaledMinHeight = ConvertToDeviceSize(cMinHeight);

        AppWindow.Changed += AppWindow_Changed;
        
        Closed += (s, e) =>
        {
            cancelDragRegionTimerEvent = true;
            dispatcherTimer.Stop();
        };
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
                    mptr->ptMinTrackSize.X = scaledMinWidth;
                    mptr->ptMinTrackSize.Y = scaledMinHeight;
                }
                break;
            }

            case PInvoke.WM_DPICHANGED:
            {
                scaleFactor = (wParam & 0xFFFF) / 96.0;
                scaledMinWidth = ConvertToDeviceSize(cMinWidth);
                scaledMinHeight = ConvertToDeviceSize(cMinHeight);
                break;
            }

            case PInvoke.WM_SYSCOMMAND when (lParam == VK_SPACE) && (AppWindow.Presenter.Kind != AppWindowPresenterKind.FullScreen):
            {
                HideSystemMenu();
                ShowSystemMenu(viaKeyboard: true);
                return (LRESULT)0;
            }

            case PInvoke.WM_NCRBUTTONUP when wParam == HTCAPTION:
            {
                HideSystemMenu();
                ShowSystemMenu(viaKeyboard: false);
                return (LRESULT)0;
            }

            case PInvoke.WM_NCLBUTTONDOWN when wParam == HTCAPTION:
            {
                HideSystemMenu();
                break;
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

        systemMenu ??= BuildSystemMenu();
        systemMenu.ShowAt(null, new Point(p.X / scaleFactor, p.Y / scaleFactor));
    }

    private void HideSystemMenu()
    {
        if ((systemMenu is not null) && systemMenu.IsOpen)
        {
            systemMenu.Hide();
        }
    }

    private MenuFlyout BuildSystemMenu()
    {
        const string cStyleKey = "DefaultMenuFlyoutPresenterStyle";
        const string cPaddingKey = "MenuFlyoutItemThemePaddingNarrow";

        Debug.Assert(Content is FrameworkElement);
        Debug.Assert(((FrameworkElement)Content).Resources.ContainsKey(cStyleKey));
        Debug.Assert(((FrameworkElement)Content).Resources.ContainsKey(cPaddingKey));

        restoreCommand = new RelayCommand(o => PostSysCommandMessage(SC.RESTORE), CanRestore);
        moveCommand = new RelayCommand(o => PostSysCommandMessage(SC.MOVE), CanMove);
        sizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.SIZE), CanSize);
        minimizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MINIMIZE), CanMinimize);
        maximizeCommand = new RelayCommand(o => PostSysCommandMessage(SC.MAXIMIZE), CanMaximize);
        closeCommand = new RelayCommand(o => PostSysCommandMessage(SC.CLOSE));

        MenuFlyout menuFlyout = new MenuFlyout()
        {
            XamlRoot = Content.XamlRoot,
            MenuFlyoutPresenterStyle = (Style)((FrameworkElement)Content).Resources[cStyleKey],
            OverlayInputPassThroughElement = Content,
        };

        // always use narrow padding (the first time the menu is opened it may use normal padding, other times narrrow)
        Thickness narrow = (Thickness)((FrameworkElement)Content).Resources[cPaddingKey];

        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = "Restore", Command = restoreCommand, Padding = narrow, AccessKey = "R" });
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = "Move", Command = moveCommand, Padding = narrow, AccessKey = "M" });
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = "Size", Command = sizeCommand, Padding = narrow, AccessKey = "S" });
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = "Minimize", Command = minimizeCommand, Padding = narrow, AccessKey = "N" });
        menuFlyout.Items.Add(new MenuFlyoutItem() { Text = "Maximize", Command = maximizeCommand, Padding = narrow, AccessKey = "X" });
        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        MenuFlyoutItem closeItem = new MenuFlyoutItem() { Text = "Close", Command = closeCommand, Padding = narrow, AccessKey = "C" };
        // the accelerator is disabled to avoid two close messages (the original system menu still exists)
        closeItem.KeyboardAccelerators.Add(new() { Modifiers = VirtualKeyModifiers.Menu, Key = VirtualKey.F4, IsEnabled = false });
        menuFlyout.Items.Add(closeItem);

        return menuFlyout;
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

    private int ConvertToDeviceSize(double value) => Convert.ToInt32(value * scaleFactor);

    private double IntialiseScaleFactor()
    {
        double dpi = PInvoke.GetDpiForWindow((HWND)windowPtr);
        return dpi / 96.0;
    }

    private void ClearWindowDragRegions()
    {
        // Guard against race hazards. If a size changed event is generated the timer will be
        // started. The drag regions could then be cleared when a context menu is opened, followed
        // by the timer event which could then reset the drag regions while the menu was still open. Stopping
        // the timer isn't enough because the tick event may have already been queued (on the same thread).
        cancelDragRegionTimerEvent = true;

        // allow mouse interaction with menu fly outs,  
        // including clicks anywhere in the client area used to dismiss the menu
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            inputNonClientPointerSource.ClearRegionRects(NonClientRegionKind.Caption);
        }
    }

    private void SetWindowDragRegionsInternal()
    {
        const int cInitialCapacity = 28;

        cancelDragRegionTimerEvent = false;

        try
        {
            if ((Content is FrameworkElement layoutRoot) && layoutRoot.IsLoaded && AppWindowTitleBar.IsCustomizationSupported())
            {
                // as there is no clear distinction any more between the title bar region and the client area,
                // just treat the whole window as a title bar, click anywhere on the backdrop to drag the window.
                RectInt32 windowRect = new RectInt32(0, 0, AppWindow.ClientSize.Width, AppWindow.ClientSize.Height);
                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, [windowRect]);

                List<RectInt32> rects = new List<RectInt32>(cInitialCapacity);
                LocatePassThroughContent(rects, layoutRoot);
                Debug.Assert(rects.Count <= cInitialCapacity);

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

    private void LocatePassThroughContent(List<RectInt32> rects, UIElement item, ScrollViewerBounds? bounds = null)
    {
        ScrollViewerBounds? parentBounds = bounds;

        foreach (UIElement child in LogicalTreeHelper.GetChildren(item))
        {
            switch (child)
            {
                case Panel: break;

                case CountdownTextBox:
                case Button:
                case TreeView:
                case ListView:
                case SplitButton:
                case NavigationViewItem:
                case Expander:
                case AutoSuggestBox:
                case TextBlock tb when ReferenceEquals(tb, tb.Tag): // it contains a hyperlink
                {
                    Point offset = GetOffsetFromXamlRoot(child);
                    Vector2 actualSize = child.ActualSize;

                    if ((parentBounds is not null) && (offset.Y < parentBounds.Top)) // top clip (for vertical scroll bars)
                    {
                        actualSize.Y -= (float)(parentBounds.Top - offset.Y);

                        if (actualSize.Y < 0.1)
                            continue;

                        offset.Y = parentBounds.Top;
                    }

                    rects.Add(ScaledRect(offset, actualSize, scaleFactor));
                    continue;
                }

                case ScrollViewer:
                {
                    // nested scroll viewers is not supported
                    bounds = new ScrollViewerBounds(GetOffsetFromXamlRoot(child), child.ActualSize);

                    if (((ScrollViewer)child).ComputedVerticalScrollBarVisibility == Visibility.Visible)
                    {
                        ScrollBar? vScrollBar = child.FindChild<ScrollBar>();

                        if (vScrollBar is not null)
                        {
                            Debug.Assert(vScrollBar.Name.Equals("VerticalScrollBar"));
                            rects.Add(ScaledRect(GetOffsetFromXamlRoot(vScrollBar), vScrollBar.ActualSize, scaleFactor));
                        }
                    }

                    break;
                }

                case CustomTitleBar ctb:
                {
                    rects.Add(ScaledRect(GetOffsetFromXamlRoot(ctb.WindowIconArea), ctb.WindowIconArea.ActualSize, scaleFactor));
                    break;
                }

                default: break;
            }

            LocatePassThroughContent(rects, child, bounds);
        }

        static Point GetOffsetFromXamlRoot(UIElement e)
        {
            GeneralTransform gt = e.TransformToVisual(null);
            return gt.TransformPoint(new Point(0, 0));
        }
    }

    private static RectInt32 ScaledRect(in Point location, in Vector2 size, double scale)
    {
        return new RectInt32(Convert.ToInt32(location.X * scale),
                             Convert.ToInt32(location.Y * scale),
                             Convert.ToInt32(size.X * scale),
                             Convert.ToInt32(size.Y * scale));
    }

    private void AddDragRegionEventHandlers(UIElement item)
    {
        foreach (UIElement child in LogicalTreeHelper.GetChildren(item))
        {
            switch (child)
            {
                case Panel: break;

                case SplitButton sb:
                {
                    if (sb.Flyout is not null)
                    {
                        sb.Flyout.Opened += Flyout_Opened;
                        sb.Flyout.Closed += Flyout_Closed;
                    }
                    continue;
                }

                case TreeView:
                case ListView:
                {
                    if (child.ContextFlyout is not null)
                    {
                        child.ContextFlyout.Opened += Flyout_Opened;
                        child.ContextFlyout.Closed += Flyout_Closed;
                    }
                    continue;
                }

                case Expander expander:
                {
                    expander.SizeChanged += Expander_SizeChanged;
                    continue;
                }

                case AutoSuggestBox autoSuggestBox:
                {
                    Popup? popup = autoSuggestBox.FindChild<Popup>();

                    if (popup is not null)
                    {
                        popup.Opened += Flyout_Opened;
                        popup.Closed += Flyout_Closed;
                    }
                    continue;
                }

                case ScrollViewer scrollViewer:
                {
                    scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
                    break;
                }

                case Button:
                case GroupBox:
                case TextBlock:
                case CustomTitleBar:
                case NavigationViewItem: continue;

                default: break;
            }

            AddDragRegionEventHandlers(child);
        }

        void Flyout_Opened(object? sender, object e) => ClearWindowDragRegions();
        void Flyout_Closed(object? sender, object e) => SetWindowDragRegionsInternal();
        void Expander_SizeChanged(object sender, SizeChangedEventArgs e) => SetWindowDragRegionsInternal();
        void ScrollViewer_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e) => SetWindowDragRegions();
    }

    private DispatcherTimer InitialiseDragRegionTimer()
    {
        DispatcherTimer dt = new DispatcherTimer();
        dt.Interval = TimeSpan.FromMilliseconds(50);
        dt.Tick += DispatcherTimer_Tick;
        return dt;
    }

    private void SetWindowDragRegions()
    {
        // defer setting the drag regions while still resizing the window or scrolling
        // it's content. If the timer is already running, this resets the interval.
        dispatcherTimer.Start();
    }

    private void DispatcherTimer_Tick(object? sender, object e)
    {
        dispatcherTimer.Stop();

        if (!cancelDragRegionTimerEvent)
        {
            SetWindowDragRegionsInternal();
        }
    }
}

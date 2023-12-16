﻿// included here due to name conflicts
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;

using RelayCommand = Countdown.ViewModels.RelayCommand;

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

    protected readonly InputNonClientPointerSource inputNonClientPointerSource;
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

                List<RectInt32> rects = LocatePassThroughContent(layoutRoot);
                inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, rects.ToArray());
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }


#if false

    <Canvas x:Name="cvb" Grid.RowSpan="2"/>

    private void UpdateCanvas(Canvas canvas, List<RectInt32> rects)
    {
        canvas.Children.Clear();

        foreach (RectInt32 r in rects)
        {
            RectInt32 sr = ScaledRect(r.X, r.Y, r.Width, r.Height, 1.0 / canvas.XamlRoot.RasterizationScale);

            Rectangle shape = new Rectangle()
            {
                Height = sr.Height,
                Width = sr.Width,
                Fill = new SolidColorBrush()
                {
                    Color = new Color() { A = 0x33, R = 0xFF, G = 0x00, B = 0x00, },
                },
            };

            Canvas.SetLeft(shape, sr.X);
            Canvas.SetTop(shape, sr.Y);

            canvas.Children.Add(shape);
        }
    }
#endif

    private record class ScrollViewerBounds(in Point Offset, in Vector2 Size)
    {
        public double Top => Offset.Y;
    }

    private static List<RectInt32> LocatePassThroughContent(DependencyObject reference, ScrollViewerBounds? bounds = null)
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

        List<RectInt32> rects = new List<RectInt32>();

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
                case TextBlock when (((TextBlock)element).Inlines.FirstOrDefault(x => x is Hyperlink) is not null):
                {
                    Point offset = GetOffsetFromXamlRoot(element);
                    Vector2 actualSize = element.ActualSize;

                    if ((bounds is not null) && (offset.Y < bounds.Top)) // for this ui, only top clip is required 
                    {
                        actualSize.Y -= (float)(bounds.Top - offset.Y);
                        offset.Y = bounds.Top;
                    }

                    rects.Add(ScaledRect(offset, actualSize, element.XamlRoot.RasterizationScale));
                    return rects;
                }

                case ScrollViewer:
                {
                    // chained scroll viewers is not supported
                    bounds = new ScrollViewerBounds(GetOffsetFromXamlRoot(element), element.ActualSize);
                    break;
                }

                default: break;
            }

            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(reference); index++)
            {
                DependencyObject current = VisualTreeHelper.GetChild(reference, index);
                rects.AddRange(LocatePassThroughContent(current, bounds));
            }
        }

        return rects;
    }

    private static RectInt32 ScaledRect(in Point location, in Vector2 size, double scale)
    {
        return ScaledRect(location.X, location.Y, size.X, size.Y, scale);
    }

    private static RectInt32 ScaledRect(double x, double y, double width, double height, double scale)
    {
        return new RectInt32(Convert.ToInt32(x * scale),
                                Convert.ToInt32(y * scale),
                                Convert.ToInt32(width * scale),
                                Convert.ToInt32(height * scale));
    }
}

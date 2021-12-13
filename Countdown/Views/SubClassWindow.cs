namespace Countdown.Views;

// this code is largely based on https://github.com/marb2000/DesktopWindow
// but modified to use CsWin32 windows API generator rather than using hard
// wired interop definitions

internal class SubClassWindow : Window
{
    public double MinWidth { get; set; }
    public double MinHeight { get; set; }

    private readonly HWND hWnd;
    private readonly WNDPROC newWndDelegate;
    private readonly WNDPROC oldWndDelegate;

    public event CancelEventHandler? WindowClosing;

    public SubClassWindow()
    {
        IntPtr handle = WindowNative.GetWindowHandle(this);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException();

        hWnd = (HWND)handle;

        newWndDelegate = new WNDPROC(NewWindowProc);
        IntPtr newProcPtr = Marshal.GetFunctionPointerForDelegate(newWndDelegate);
        IntPtr oldProcPtr;

        if (IntPtr.Size == 8)
            oldProcPtr = PInvoke.SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, newProcPtr);
        else
            oldProcPtr = (IntPtr)PInvoke.SetWindowLong(hWnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, newProcPtr.ToInt32());

        if (oldProcPtr == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        oldWndDelegate = Marshal.GetDelegateForFunctionPointer<WNDPROC>(oldProcPtr);

        SetWindowIcon();
    }


    private LRESULT NewWindowProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam)
    {
        const uint WM_GETMINMAXINFO = 0x0024;
        const uint WM_CLOSE = 0x0010;

        if (Msg == WM_GETMINMAXINFO)
        {
            uint dpi = PInvoke.GetDpiForWindow(hWnd);
            double scalingFactor = dpi / 96.0;

            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.x = (int)(MinWidth * scalingFactor);
            minMaxInfo.ptMinTrackSize.y = (int)(MinHeight * scalingFactor);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }
        else if (Msg == WM_CLOSE)
        {
            CancelEventArgs e = new CancelEventArgs();  
            OnWindowClosing(e);

            if (e.Cancel)
                return new LRESULT(0);
        }

        return PInvoke.CallWindowProc(oldWndDelegate, hWnd, Msg, wParam, lParam);
    }


    protected Size WindowSize
    {
        set
        {
            uint dpi = PInvoke.GetDpiForWindow(hWnd);
            double scalingFactor = dpi / 96.0;

            BOOL result = PInvoke.SetWindowPos(hWnd, (HWND)0, 0, 0, (int)(value.Width * scalingFactor), (int)(value.Height * scalingFactor), SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

            if (result.Value == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }


    private void SetWindowIcon()
    {
        BOOL result = PInvoke.GetModuleHandleEx(0, null, out FreeLibrarySafeHandle module);

        if ((result.Value == 0) || module.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        WPARAM ICON_SMALL = 0;
        WPARAM ICON_BIG = 1;
        const string appIconResourceId = "#32512";

        SetWindowIcon(module, appIconResourceId, ICON_SMALL, PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON));
        SetWindowIcon(module, appIconResourceId, ICON_BIG, PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXICON));
    }


    private void SetWindowIcon(FreeLibrarySafeHandle module, string iconId, WPARAM iconType, int size)
    {
        const uint WM_SETICON = 0x0080;

        SafeFileHandle hIcon = PInvoke.LoadImage(module, iconId, GDI_IMAGE_TYPE.IMAGE_ICON, size, size, IMAGE_FLAGS.LR_DEFAULTCOLOR);

        if (hIcon.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        bool refAdded = false;
        LRESULT result = new LRESULT(0);

        try
        {
            hIcon.DangerousAddRef(ref refAdded);

            if (refAdded)
            {
                result = PInvoke.SendMessage(hWnd, WM_SETICON, iconType, hIcon.DangerousGetHandle());

                if (result.Value != 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            // only decrement the SafeHandle reference count if SendMessage fails 
            if ((result.Value != 0) && refAdded)
                hIcon.DangerousRelease();
        }
    }


    protected void CenterInPrimaryDisplay()
    {
        BOOL result = PInvoke.GetWindowRect(hWnd, out RECT lpRect);
        
        if (result.Value == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        DisplayArea primary = DisplayArea.Primary;

        int top = (primary.WorkArea.Height - (lpRect.bottom - lpRect.top)) / 2;
        int left = (primary.WorkArea.Width - (lpRect.right - lpRect.left)) / 2;

        top = Math.Max(top, 0); // guarantee the title bar is visible
        left = Math.Max(left, 0);

        result = PInvoke.SetWindowPos(hWnd, (HWND)0, left, top, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

        if (result.Value == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    protected static string GetSettingsFilePath()
    {
        const string cSettingsFileName = "{0B8391E1-FEB9-46CD-A38E-C66984A4A160}.json";
        const string cSettingsDirName = "Countdown";

        Guid FOLDERID_LocalAppData = new Guid("{F1B32785-6FBA-4FCF-9D55-7B8E7F157091}");
        HRESULT result = PInvoke.SHGetKnownFolderPath(FOLDERID_LocalAppData, (uint)KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, out PWSTR ppszPath);

        if (result.Value != 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        string path = Path.Join(ppszPath.ToString(), cSettingsDirName, cSettingsFileName);

        // SHGetKnownFolderPath() allocates memory for the path, ToString() duplicates it
        unsafe { PInvoke.CoTaskMemFree(ppszPath.Value); }

        return path;
    }


    protected WINDOWPLACEMENT GetWindowPlacement()
    {
        WINDOWPLACEMENT placement = default;

        BOOL result = PInvoke.GetWindowPlacement(hWnd, ref placement);

        if (result.Value == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return placement;
    }


    protected void SetWindowPlacement(WINDOWPLACEMENT placement)
    {
        if (placement.length == 0)  // first time, no saved state
        {
            WindowSize = new Size(MinWidth, MinHeight);
            CenterInPrimaryDisplay();
            Activate();
        }
        else
        {
            if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED)
                placement.showCmd = SHOW_WINDOW_CMD.SW_SHOWNORMAL;

            // calling SetWindowPlacement() also activates the window
            BOOL result = PInvoke.SetWindowPlacement(hWnd, placement);

            if (result.Value == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }


    protected void OnWindowClosing(CancelEventArgs args)
    {
        WindowClosing?.Invoke(this, args);
    }
}

namespace Countdown.Views;

// this code is largely based on https://github.com/marb2000/DesktopWindow
// but modified to use CsWin32 windows API generator rather than using hard
// wired interop definitions

internal class SubClassWindow : Window
{
    public double MinWidth { get; set; }
    public double MinHeight { get; set; }

    private const int S_OK = 0;

    private readonly HWND hWnd;
    private readonly SUBCLASSPROC subClassDelegate;

    public event CancelEventHandler? WindowClosing;

    public SubClassWindow()
    {
        IntPtr handle = WindowNative.GetWindowHandle(this);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException();

        hWnd = (HWND)handle;
        subClassDelegate = new SUBCLASSPROC(NewSubWindowProc);

        if (!PInvoke.SetWindowSubclass(hWnd, subClassDelegate, 0, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        SetWindowIcon();
    }


    private LRESULT NewSubWindowProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
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
                return new LRESULT(S_OK);
        }

        return PInvoke.DefSubclassProc(hWnd, Msg, wParam, lParam);
    }


    protected Size WindowSize
    {
        set
        {
            uint dpi = PInvoke.GetDpiForWindow(hWnd);
            double scalingFactor = dpi / 96.0;

            if (!PInvoke.SetWindowPos(hWnd, (HWND)0, 0, 0, (int)(value.Width * scalingFactor), (int)(value.Height * scalingFactor), SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }


    private void SetWindowIcon()
    {
        if (!PInvoke.GetModuleHandleEx(0, null, out FreeLibrarySafeHandle module))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

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
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        Marshal.SetLastPInvokeError(S_OK);

        try
        {
            LRESULT previousIcon = PInvoke.SendMessage(hWnd, WM_SETICON, iconType, hIcon.DangerousGetHandle());
            Debug.Assert(previousIcon == (LRESULT)0);
        }
        finally
        {
            hIcon.SetHandleAsInvalid(); // SafeFileHandle must not release the shared icon
        }

        if (Marshal.GetLastPInvokeError() != S_OK)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }


    protected void CenterInPrimaryDisplay()
    {
        if (!PInvoke.GetWindowRect(hWnd, out RECT lpRect))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        DisplayArea primary = DisplayArea.Primary;

        int top = (primary.WorkArea.Height - (lpRect.bottom - lpRect.top)) / 2;
        int left = (primary.WorkArea.Width - (lpRect.right - lpRect.left)) / 2;

        top = Math.Max(top, 0); // guarantee the title bar is visible
        left = Math.Max(left, 0);

        if (!PInvoke.SetWindowPos(hWnd, (HWND)0, left, top, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    protected static string GetSettingsFilePath()
    {
        const string cSettingsFileName = "{0B8391E1-FEB9-46CD-A38E-C66984A4A160}.json";
        const string cSettingsDirName = "Countdown";

        Guid FOLDERID_LocalAppData = new Guid("{F1B32785-6FBA-4FCF-9D55-7B8E7F157091}");
        HRESULT result = PInvoke.SHGetKnownFolderPath(FOLDERID_LocalAppData, (uint)(KNOWN_FOLDER_FLAG.KF_FLAG_CREATE | KNOWN_FOLDER_FLAG.KF_FLAG_INIT), null, out PWSTR ppszPath);

        if (result.Failed)
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        string path = Path.Join(ppszPath.ToString(), cSettingsDirName, cSettingsFileName);

        // SHGetKnownFolderPath() allocates memory for the path, ToString() duplicates it
        unsafe { PInvoke.CoTaskMemFree(ppszPath.Value); }

        return path;
    }


    protected WINDOWPLACEMENT GetWindowPlacement()
    {
        WINDOWPLACEMENT placement = default;

        if (!PInvoke.GetWindowPlacement(hWnd, ref placement))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

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

            // SetWindowPlacement() also activates the window
            if (!PInvoke.SetWindowPlacement(hWnd, placement))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }


    protected void OnWindowClosing(CancelEventArgs args)
    {
        WindowClosing?.Invoke(this, args);
    }
}

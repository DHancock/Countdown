namespace Countdown.Views
{
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
        }


        private LRESULT NewWindowProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam)
        {
            const uint WM_GETMINMAXINFO = 0x0024;

            if (Msg == WM_GETMINMAXINFO)
            {
                uint dpi = PInvoke.GetDpiForWindow(hWnd);
                double scalingFactor = dpi / 96.0;

                MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                minMaxInfo.ptMinTrackSize.x = (int)(MinWidth * scalingFactor);
                minMaxInfo.ptMinTrackSize.y = (int)(MinHeight * scalingFactor);
                Marshal.StructureToPtr(minMaxInfo, lParam, true);
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

        protected string Icon
        {
            set
            {
                try
                {
                    WPARAM ICON_SMALL = 0;
                    WPARAM ICON_BIG = 1;

                    LoadIcon(value, ICON_SMALL, PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON));
                    LoadIcon(value, ICON_BIG, PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXICON));
                }
                catch (Win32Exception)
                {
                }
            }
        }

        private void LoadIcon(string fileName, WPARAM iconType, int size)
        {
            const uint WM_SETICON = 0x0080;

            SafeFileHandle hIcon = PInvoke.LoadImage(null, fileName, GDI_IMAGE_TYPE.IMAGE_ICON, size, size, IMAGE_FLAGS.LR_LOADFROMFILE);

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
    }
}

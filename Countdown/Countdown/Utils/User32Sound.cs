using Windows.Win32;

namespace Countdown.Utils
{
    internal static class User32Sound
    {
        private const uint MB_ICONHAND = 0x0010;
        private const uint MB_ICONQUESTION = 0x0020;
        private const uint MB_ICONEXCLAMATION = 0x0030;
        private const uint MB_ICONASTERISK = 0x0040;

        public static void PlayExclamation()
        {
            _ = PInvoke.MessageBeep(MB_ICONEXCLAMATION);
        }
    }
}


namespace Countdown.Utils;


internal static class SystemParameters
{

    public static unsafe bool WindowAnimationsEnabled
    {
        get
        {
            BOOL animations = false;
            BOOL result = PInvoke.SystemParametersInfo(SYSTEM_PARAMETERS_INFO_ACTION.SPI_GETCLIENTAREAANIMATION, 0, &animations, 0);
            
            if (result.Value == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return animations;
        }
    }
}

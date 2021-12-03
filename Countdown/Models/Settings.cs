namespace Countdown.Models;

internal static class Settings
{
    private static bool sImplemented = true;

    // fallback store for values if ApplicationData isn't implemented
    private static int sChooseNumbersIndex = 1;
    private static int sChooseLettersIndex = 1;
    private static ElementTheme sCurrentTheme = ElementTheme.Default;


    public static int ChooseNumbersIndex
    {
        get => GetValue(sChooseNumbersIndex);
        set => SetValue(value, ref sChooseNumbersIndex);    
    }

    public static int ChooseLettersIndex
    {
        get => GetValue(sChooseLettersIndex);
        set => SetValue(value, ref sChooseLettersIndex);
    }

    public static ElementTheme CurrentTheme
    {
        get => GetValue(sCurrentTheme);
        set => SetValue(value, ref sCurrentTheme);
    }



    private static T GetValue<T>(T fallbackValue, [CallerMemberName] string key = "")
    {
        if (sImplemented)
        {
            try
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out object? value))
                    return (T)value;
            }
            catch (InvalidOperationException) // it hasn't been implemented in WindowsAppSDK 1.0.0
            {
                sImplemented = false;
            }
        }

        return fallbackValue;
    }


    private static void SetValue<T>(T value, ref T fallbackStore, [CallerMemberName] string key = "")
    {
        if (sImplemented)
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values[key] = value;
            }
            catch (InvalidOperationException) // it hasn't been implemented in WindowsAppSDK 1.0.0
            {
                sImplemented = false;
            }
        }

        if (!sImplemented)
            fallbackStore = value;
    }
}

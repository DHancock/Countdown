namespace Countdown.Models;

// These settings are serialized to a json text file.
// Adding or deleting properties is safe. The missing, or extra data is ignored.
// Changing the type of an existing property may cause problems though. Best not
// delete properties just in case a name is later reused with a different type.

internal class Settings
{
    public int ChooseNumbersIndex { get; set; } = 1;
    public int ChooseLettersIndex { get; set; } = 1;
    public ElementTheme CurrentTheme { get; set; } = ElementTheme.Default;

    public WINDOWPLACEMENT WindowPlacement { get; set; } = default;
}


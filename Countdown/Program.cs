namespace Countdown;

public static class Program
{
    [STAThread]
    static void Main()
    {
        // Create the installer mutexes with current user access.
        // The app is installed per user rather than all users.
        const string name = "06482883-F905-4F5C-88E1-3B6B328144DD";

        PInvoke.CreateMutex(null, false, name);
        PInvoke.CreateMutex(null, false, "Global\\" + name);

        XamlGeneratedProgram.XamlGeneratedMain();
    }
}
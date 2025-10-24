using Countdown.Views;

namespace Countdown;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static App Instance => (App)Current;

    private readonly SafeHandle localMutex;
    private readonly SafeHandle globalMutex;

    private MainWindow? m_window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        // Create the installer mutexes with current user access. The app is installed per
        // user rather than all users.
        const string name = "06482883-F905-4F5C-88E1-3B6B328144DD";
        localMutex = PInvoke.CreateMutex(null, false, name);
        globalMutex = PInvoke.CreateMutex(null, false, "Global\\" + name);

        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow("Countdown");
    }

    internal static MainWindow MainWindow
    {
        get
        {
            Debug.Assert(Instance.m_window is not null);
            return Instance.m_window;
        }
    }

    public static string GetAppDataPath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Join(localAppData, "countdown.davidhancock.net");
    }
}

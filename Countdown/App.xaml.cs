using Countdown.Views;

namespace Countdown;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public const string cDisplayName = "Countdown";
    public const string cIconResourceID = "32512";
    public static bool IsPackaged { get; } = GetIsPackaged();

    private Window? m_window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
    }

    internal static MainWindow? MainWindow { get => (MainWindow?)((App)Current).m_window; }

    private static bool GetIsPackaged()
    {
        uint length = 0;
        WIN32_ERROR error = PInvoke.GetCurrentPackageFullName(ref length, null);
        return error == WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER;
    }
}

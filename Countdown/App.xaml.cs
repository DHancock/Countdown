using Countdown.Views;

// valid c# casts would otherwise fail for these types in AOT builds due to trimming (using CsWinRT 2.3.1)
//
// While CsWinRT provides a DynamicWindowsRuntimeCast attribute and can add it automatically when fixing warnings
// generated when CsWinRTAotWarningLevel is set 3, the attribute fails to prevent structs from being trimmed e.g. Thickness
//
// The CsWinRT analyzer won't identify types specified for generic functions so will not correct those calls.
// The attribute has to be added manually to calls to FindChild<TreeView>() other wise the TreeView type may be trimmed.
//
// When WinUi supports CsWinRT 3.0 this should all go away...
[assembly: GeneratedWinRTExposedExternalType(typeof(Grid))]
[assembly: GeneratedWinRTExposedExternalType(typeof(Border))]
[assembly: GeneratedWinRTExposedExternalType(typeof(AppBarButton))]
[assembly: GeneratedWinRTExposedExternalType(typeof(TextCommandBarFlyout))]
[assembly: GeneratedWinRTExposedExternalType(typeof(OverlappedPresenter))]
[assembly: GeneratedWinRTExposedExternalType(typeof(TreeViewList))]
[assembly: GeneratedWinRTExposedExternalType(typeof(Microsoft.UI.Xaml.Controls.Primitives.ScrollBar))]

namespace Countdown;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    internal static App Instance => (App)Current;

    private MainWindow? m_window;

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

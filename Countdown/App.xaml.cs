﻿using Countdown.Views;

namespace Countdown;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        //if (!Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.TryInitialize(0x00010000, out int hresult))
        //    System.Diagnostics.Debug.Fail($"Bootstrap initialise failed: {hresult}");
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    public static Window? MainWindow { get => ((App)Current).m_window; }

    private Window? m_window;
}
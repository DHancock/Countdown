using Microsoft.UI.Dispatching;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using SdkRelease = Microsoft.WindowsAppSDK.Release;
using RuntimeVersion = Microsoft.WindowsAppSDK.Runtime.Version;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Countdown;

public static class Program
{
    [STAThread]
    static void Main()
    {
        if (InitializeWinAppSdk())
        {
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });

            Bootstrap.Shutdown();
        }
    }

    private static bool InitializeWinAppSdk()
    {
        uint sdkVersion = SdkRelease.MajorMinor;
        PackageVersion minRuntimeVersion = new PackageVersion(RuntimeVersion.Major, RuntimeVersion.Minor, RuntimeVersion.Build);
        Bootstrap.InitializeOptions options = Bootstrap.InitializeOptions.OnNoMatch_ShowUI;

        return Bootstrap.TryInitialize(sdkVersion, null, minRuntimeVersion, options, out _);
    }
}

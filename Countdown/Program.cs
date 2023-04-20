using Microsoft.UI.Dispatching;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Countdown;

public static class Program
{
    [STAThread]
    static void Main()
    {
        if (Bootstrap.TryInitialize(FindRollForwardSdkVersion(3000), out int hresult))  
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


    // At this stage there should be at least one version of the WinAppSdk installed.
    // It either already existed or has been installed by the installer 
    // Each platform has it's own install.
    //
    // All WinAppSdk versions 1.2.n has a major package version of 2000
    // All WinAppSdk versions 1.3.n has a major package version of 3000 etc.
    //
    // Unfortunately the bootstrapper also enumerates packages, but it's not quite so promiscuous
    // See https://github.com/microsoft/WindowsAppSDK/blob/main/dev/WindowsAppRuntime_BootstrapDLL/MddBootstrap.cpp
    // On my middle of the road laptop this adds less than 200ms to the app's start time
    private static uint FindRollForwardSdkVersion(uint minimumPackageMajorVersion)
    {
        const string cMicrosoft = "8wekyb3d8bbwe";

        object lockObject = new object();
        PackageManager packageManager = new PackageManager();
        ProcessorArchitecture architecture = GetProcessorArchitecture();

        uint latestPackageMajorVersion = minimumPackageMajorVersion;

        Parallel.ForEach(packageManager.FindPackagesForUserWithPackageTypes("", PackageTypes.Main), package =>
        {
            if ((package.Id.Architecture == architecture) &&
                (package.Id.Version.Major > minimumPackageMajorVersion) &&
                string.Equals(package.Id.PublisherId, cMicrosoft, StringComparison.Ordinal) &&
                package.Id.FullName.StartsWith("Microsoft.WinAppRuntime.DDLM.") &&
                (package.Dependencies.Count == 1))
            {
                // check the DDLM package has a dependency on a framework package
                Package dependency = package.Dependencies[0];

                if (dependency.IsFramework &&
                    dependency.Id.FullName.StartsWith("Microsoft.WindowsAppRuntime.1."))
                {
                    lock(lockObject)
                    {
                        if (latestPackageMajorVersion < dependency.Id.Version.Major)
                            latestPackageMajorVersion = dependency.Id.Version.Major;
                    }
                }
            }
        });

        return 0x00010000 + (latestPackageMajorVersion / 1000);
    }

    private static ProcessorArchitecture GetProcessorArchitecture()
    {
        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86: return ProcessorArchitecture.X86;
            case Architecture.X64: return ProcessorArchitecture.X64;
            case Architecture.Arm64: return ProcessorArchitecture.Arm64;

            default: return ProcessorArchitecture.Unknown;
        }
    }
}

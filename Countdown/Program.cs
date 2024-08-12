using Microsoft.UI.Dispatching;

using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Countdown;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if ((args.Length == 1) && (args[0] == "/uninstall"))
        {
            DeleteAppData();
            KillOtherProcesses();
        }
        else
        {             
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
    }

    private static void DeleteAppData()
    {
        try
        {
            DirectoryInfo di = new DirectoryInfo(App.GetAppDataPath());
            di.Delete(true);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }
    }

    private static void KillOtherProcesses()
    {
        try
        {
            Process thisProcess = Process.GetCurrentProcess();

            foreach (Process process in Process.GetProcessesByName(thisProcess.ProcessName))
            {
                if ((process.Id != thisProcess.Id) && (process.MainModule?.FileName == thisProcess.MainModule?.FileName))
                {
                    process.Kill(); // ensure uninstall is able to complete
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex.ToString());
        }
    }
}
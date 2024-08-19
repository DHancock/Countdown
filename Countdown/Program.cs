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
            KillOtherProcessesSync();
            DeleteAppData();
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
            Debug.WriteLine(ex.ToString());
        }
    }

    private static void KillOtherProcessesSync() // ensure uninstall is able to complete
    {
        try
        {
            Process thisProcess = Process.GetCurrentProcess();

            List<Process> killedProcesses = new List<Process>();

            foreach (Process process in Process.GetProcessesByName(thisProcess.ProcessName))
            {
                if ((process.Id != thisProcess.Id) && (process.MainModule?.FileName == thisProcess.MainModule?.FileName))
                {
                    try
                    {
                        process.Kill(); 
                        killedProcesses.Add(process);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
            }

            foreach (Process process in killedProcesses) 
            {
                try
                {
                    // cannot use async version due to main entry point
                    process.WaitForExit(); 
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }
}
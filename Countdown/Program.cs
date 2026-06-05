using Microsoft.UI.Dispatching;

using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

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

        Application.Start((p) =>
        {
            DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }
}
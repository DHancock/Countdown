using Microsoft.UI.Dispatching;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Countdown;

public static class Program
{
    [STAThread]
    static void Main()
    {
        if (Bootstrap.TryInitialize(0x00010003, out int hresult))  
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
}

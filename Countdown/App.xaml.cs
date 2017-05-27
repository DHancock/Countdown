using System.Windows;


namespace Countdown
{
    public partial class App : Application
    {
        public App() : base()
        {
            Exit += (s, e) => Settings.Default.Save();
        }
    }
}

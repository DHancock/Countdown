using Countdown.Models;
using Microsoft.UI.Xaml;
using System.Windows.Input;


namespace Countdown.ViewModels
{
    internal sealed class SettingsViewModel : PropertyChangedBase
    {

        public ElementTheme SelectedTheme
        {
            get => Settings.CurrentTheme;

            set
            {
                if (App.MainWindow?.Content is FrameworkElement fe)
                {
                    fe.RequestedTheme = value;
                    Settings.CurrentTheme = value;
                }
            }
        }


        public bool IsLightTheme
        {
            get { return SelectedTheme == ElementTheme.Light; }
            set { if (value) SelectedTheme = ElementTheme.Light; }
        }

        public bool IsDarkTheme
        {
            get { return SelectedTheme == ElementTheme.Dark; }
            set { if (value) SelectedTheme = ElementTheme.Dark; }
        }

        public bool IsSystemTheme
        {
            get { return SelectedTheme == ElementTheme.Default; }
            set { if (value) SelectedTheme = ElementTheme.Default; }
        }
    }
}


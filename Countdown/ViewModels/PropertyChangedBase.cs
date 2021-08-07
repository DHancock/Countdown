using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Countdown.ViewModels
{
    abstract internal class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void HandlePropertyChanged<T>(ref T propertyValue, T newValue, [CallerMemberName] string propertyName = "") where T : struct
        {
            if (!propertyValue.Equals(newValue))
            {
                propertyValue = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

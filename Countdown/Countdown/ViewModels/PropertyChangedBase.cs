namespace Countdown.ViewModels
{
    abstract internal class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool HandlePropertyChanged<T>(ref T propertyValue, T newValue, [CallerMemberName] string propertyName = "")
        {
            if ((propertyValue is null) || (!propertyValue.Equals(newValue)))
            {
                propertyValue = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }
    }
}

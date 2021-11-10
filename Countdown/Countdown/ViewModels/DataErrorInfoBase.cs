namespace Countdown.ViewModels
{
    abstract internal class DataErrorInfoBase : PropertyChangedBase, INotifyDataErrorInfo
    {
        // the key is the property name 
        private readonly Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();


        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        private void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }


        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !errors.ContainsKey(propertyName))
                return new List<string>();

            return errors[propertyName];
        }

        public bool HasErrors
        {
            get { return errors.Count > 0; }
        }



        /// <summary>
        /// In this implementation only one error message per property
        /// is allowed. Raises errors changed event if the message
        /// isn't already in the dictionary
        /// </summary>
        /// <param name="key">the property name</param>
        /// <param name="message"></param>
        protected void SetValidationError(string key, string message)
        {
            bool addEntry = true;

            // check if the message already exists first
            if (errors.TryGetValue(key, out List<string>? list))
                addEntry = (list is null) || (list.Count != 1) || (list[0] != message);

            if (addEntry)
            {
                errors[key] = new List<string>() { message };
                RaiseErrorsChanged(key);
            }
        }


        /// <summary>
        /// Clear the error for a property if it exists and 
        /// raises the errors changed event
        /// </summary>
        /// <param name="key">the property name</param>
        protected void ClearValidationError(string key)
        {
            if (errors.Remove(key))
                RaiseErrorsChanged(key);
        }
    }
}


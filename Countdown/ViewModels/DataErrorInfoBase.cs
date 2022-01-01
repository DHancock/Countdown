namespace Countdown.ViewModels;

abstract internal class DataErrorInfoBase : PropertyChangedBase
{
    public readonly ObservableCollection<string> ErrorText;

    public DataErrorInfoBase(int propertyCount)
    {
        ErrorText = new ObservableCollection<string>();

        for (int index = 0; index < propertyCount; index++)
            ErrorText.Add(string.Empty);
    }


    public bool HasErrors => ErrorText.Any(x => !string.IsNullOrEmpty(x));
    

    protected void SetValidationError(int index, string message)
    {
        if (string.CompareOrdinal(ErrorText[index], message) != 0)
            ErrorText[index] = message;
    }


    protected void ClearValidationError(int index)
    {
        ErrorText[index] = string.Empty;
    }


    protected void ClearAllErrors()
    {
        for (int index = 0; index < ErrorText.Count; index++)
            ClearValidationError(index);
    }
}

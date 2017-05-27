namespace Countdown.ViewModels
{
    // a simple clipboard interface that allows these methods to
    // be impersonated in unit test code which may not be running
    // in the correct thread apartment (STA is required)
    internal interface IClipboardService
    {
        string GetText();
        void SetText(string text);
    }
}

using System.Windows;

namespace Countdown.ViewModels
{
    /// <summary>
    /// Maps the service interface to the actual clipboard calls
    /// </summary>
    internal sealed class RealClipboard : IClipboardService
    {
        public string GetText() => Clipboard.GetText();
        public void SetText(string text) => Clipboard.SetText(text);
    }
}

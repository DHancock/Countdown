using Countdown.ViewModels;


namespace Countdown.UnitTests
{
    // replace the clipboard with this simplistic version that
    // works in all thread apartment types. 
    public class UnitTestClipboard : IClipboardService
    {
        private string data;

        public string GetText() => data;
        
        public void SetText(string text)
        {
            data = text;
        }
    }
}

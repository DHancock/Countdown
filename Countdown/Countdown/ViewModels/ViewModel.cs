using Countdown.Models;

namespace Countdown.ViewModels
{
    internal sealed class ViewModel
    {
        public NumbersViewModel NumbersViewModel { get; }
        public LettersViewModel LettersViewModel { get; }
        public ConundrumViewModel ConundrumViewModel { get; }
        public StopwatchViewModel StopwatchViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }



        public ViewModel()
        {
            Model model = new Model();
            StopwatchController sc = new StopwatchController();

            NumbersViewModel = new NumbersViewModel(model, sc);
            LettersViewModel = new LettersViewModel(model, sc);
            ConundrumViewModel = new ConundrumViewModel(model, sc);
            StopwatchViewModel = new StopwatchViewModel(sc);
            SettingsViewModel = new SettingsViewModel();
        }
    }
}
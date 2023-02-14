using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed class ViewModel
{
    public NumbersViewModel NumbersViewModel { get; }
    public LettersViewModel LettersViewModel { get; }
    public ConundrumViewModel ConundrumViewModel { get; }
    public StopwatchViewModel StopwatchViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }


    public ViewModel()
    {
        NumberModel numberMmodel = new NumberModel();
        WordModel wordModel = new WordModel();
        StopwatchController sc = new StopwatchController();

        NumbersViewModel = new NumbersViewModel(numberMmodel, sc);
        LettersViewModel = new LettersViewModel(wordModel, sc);
        ConundrumViewModel = new ConundrumViewModel(wordModel, sc);
        StopwatchViewModel = new StopwatchViewModel(sc);
        SettingsViewModel = new SettingsViewModel(sc);
    }
}

using Countdown.Models;

namespace Countdown.ViewModels;

internal sealed class ViewModel
{
    public NumbersViewModel NumbersViewModel { get; }
    public LettersViewModel LettersViewModel { get; }
    public ConundrumViewModel ConundrumViewModel { get; }
    public StopwatchViewModel StopwatchViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    private readonly Model model;

    public ViewModel(string settingsData)
    {
        model = new Model(DeserializeSettings(settingsData));

        StopwatchController sc = new StopwatchController();

        NumbersViewModel = new NumbersViewModel(model, sc);
        LettersViewModel = new LettersViewModel(model, sc);
        ConundrumViewModel = new ConundrumViewModel(model, sc);
        StopwatchViewModel = new StopwatchViewModel(sc);
        SettingsViewModel = new SettingsViewModel(model);
    }


    private static Settings DeserializeSettings(string data)
    {
        if (!string.IsNullOrWhiteSpace(data))
        {
            try
            {
                Settings? settings = JsonSerializer.Deserialize<Settings>(data);

                if (settings is not null)
                    return settings;
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
            }
        }
        
        return new Settings();
    }

    public string SerializeSettings() => JsonSerializer.Serialize(model.Settings);
}

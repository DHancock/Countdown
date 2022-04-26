using Countdown.Models;
using Countdown.Utils;

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
                Settings? settings = JsonSerializer.Deserialize<Settings>(data, GetSerializerOptions());

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

    public string SerializeSettings() => JsonSerializer.Serialize(model.Settings, GetSerializerOptions());

    public void UpdateWindowPlacement(WINDOWPLACEMENT placement) => model.Settings.WindowPlacement = placement;

    public WINDOWPLACEMENT GetSavedWindowPlacement() => model.Settings.WindowPlacement;

    private static JsonSerializerOptions GetSerializerOptions()
    {
        JsonSerializerOptions serializerOptions = new JsonSerializerOptions();
        serializerOptions.WriteIndented = true;

        serializerOptions.Converters.Add(new WINDOWPLACEMENTConverter());
        serializerOptions.Converters.Add(new POINTConverter());
        serializerOptions.Converters.Add(new RECTConverter());

        return serializerOptions;
    }
}

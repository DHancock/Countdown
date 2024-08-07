using Countdown.Views;

namespace Countdown.ViewModels;

// For the unpackaged variant, settings are serialized to a json text file.
// Adding or deleting properties is safe. The missing, or extra data is ignored.
// Changing the type of an existing property may cause problems though. Best not
// delete properties just in case a name is later reused with a different type.

internal class Settings
{
    public static Settings Instance = Load();
    private int volumePercentage = 50;

    public int ChooseNumbersIndex { get; set; } = 1;
    public int ChooseLettersIndex { get; set; } = 1;
    public ElementTheme CurrentTheme { get; set; } = ElementTheme.Default;

    public int VolumePercentage
    {
        get => volumePercentage;
        set
        {
            if (volumePercentage != value)
            {
                volumePercentage = value;
                VolumeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? VolumeChanged;

    public WindowState WindowState { get; set; } = WindowState.Normal;
    public RectInt32 RestoreBounds { get; set; } = default;

    [JsonIgnore]
    public bool IsFirstRun { get; private set; } = true;


    // while this breaks the singleton pattern, the code generator doesn't 
    // work with private nested classes. Worse things have happened at sea...
    public Settings()
    {
    }

    public async Task Save()
    {
        try
        {
            Directory.CreateDirectory(App.GetAppDataPath());

            string jsonString = JsonSerializer.Serialize(this, SettingsJsonContext.Default.Settings);
            await File.WriteAllTextAsync(GetSettingsFilePath(), jsonString);
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }

    internal static Settings Load()
    {
        string path = GetSettingsFilePath();

        try
        {
            string data = File.ReadAllText(path);

            if (!string.IsNullOrWhiteSpace(data))
            {
                Settings? settings = JsonSerializer.Deserialize<Settings>(data, SettingsJsonContext.Default.Settings);

                if (settings is not null)
                {
                    settings.IsFirstRun = false;
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message);
        }

        return new Settings();
    }

    private static string GetSettingsFilePath()
    {
        return Path.Join(App.GetAppDataPath(), "settings.json");
    }
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(Settings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}

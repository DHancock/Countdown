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

    public void Save()
    {
        try
        {
            string parentDir = App.GetAppDataPath();

            Directory.CreateDirectory(parentDir);

            using (FileStream fs = File.Create(GetSettingsFilePath(parentDir)))
            {
                JsonSerializer.Serialize(fs, this, SettingsJsonContext.Default.Settings);
            }
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }

    internal static Settings Load()
    {
        try
        {
            using (FileStream fs = File.OpenRead(GetSettingsFilePath(App.GetAppDataPath())))
            {
                Settings? settings = JsonSerializer.Deserialize(fs, SettingsJsonContext.Default.Settings);

                if (settings is not null)
                {
                    settings.IsFirstRun = false;
                    return settings;
                }
            }
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            Debug.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.Message);
        }

        return new Settings();
    }

    private static string GetSettingsFilePath(string parentDir)
    {
        return Path.Join(parentDir, "settings.json");
    }
}

[JsonSourceGenerationOptions(IncludeFields = true, WriteIndented = false)]
[JsonSerializable(typeof(Settings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}

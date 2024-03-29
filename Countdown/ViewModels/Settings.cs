﻿using Countdown.Views;

namespace Countdown.ViewModels;

// For the unpackaged variant, settings are serialized to a json text file.
// Adding or deleting properties is safe. The missing, or extra data is ignored.
// Changing the type of an existing property may cause problems though. Best not
// delete properties just in case a name is later reused with a different type.

internal class Settings
{
    public static Settings Data = Inner.Load();
    private int volumePercentage = 50;

    private Settings()
    {
    }

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



    public async Task Save()
    {
        await Inner.Save(this);
    }

    private sealed class Inner : Settings
    {
        // Json deserialization requires a public parameterless constructor.
        // That breaks the singleton pattern, so use a private inner inherited class
        public Inner()
        {
        }

        internal static async Task Save(Settings settings)
        {
            try
            {
                string path = GetSettingsFilePath();
                string? directory = Path.GetDirectoryName(path);
                Debug.Assert(!string.IsNullOrWhiteSpace(directory));

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, GetSerializerOptions()));
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        internal static Settings Load()
        {
            string path = GetSettingsFilePath();

            if (File.Exists(path))
            {
                try
                {
                    string data = File.ReadAllText(path);

                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        Settings? settings = JsonSerializer.Deserialize<Inner>(data, GetSerializerOptions());

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
            }

            return new Settings();
        }

        private static string GetSettingsFilePath()
        {
            const string cFileName = "settings.json";
            const string cDirName = "Countdown.davidhancock.net";
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return Path.Join(localAppData, cDirName, cFileName);
        }

        private static JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                WriteIndented = true,
                IncludeFields = true,
            };
        }
    }
}



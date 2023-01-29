using Countdown.Views;

namespace Countdown.ViewModels;

// Windows.Storage.ApplicationData isn't supported in unpackaged apps.

// For the unpackaged variant, settings are serialized to a json text file.
// Adding or deleting properties is safe. The missing, or extra data is ignored.
// Changing the type of an existing property may cause problems though. Best not
// delete properties just in case a name is later reused with a different type.

internal class Settings
{
    public static Settings Data = Inner.Load();

    private Settings()
    {
    }

    public int ChooseNumbersIndex { get; set; } = 1;
    public int ChooseLettersIndex { get; set; } = 1;
    public ElementTheme CurrentTheme { get; set; } = ElementTheme.Default;
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

        public static async Task Save(Settings settings)
        {
            if (App.IsPackaged)
                SavePackaged(settings);
            else
                await SaveUnpackaged(settings);
        }

        private static void SavePackaged(Settings settings)
        {
            try
            {
                IPropertySet properties = ApplicationData.Current.LocalSettings.Values;

                properties[nameof(ChooseNumbersIndex)] = settings.ChooseNumbersIndex;
                properties[nameof(ChooseLettersIndex)] = settings.ChooseLettersIndex;
                properties[nameof(CurrentTheme)] = (int)settings.CurrentTheme;
                properties[nameof(WindowState)] = (int)settings.WindowState;
                properties[nameof(RectInt32.X)] = settings.RestoreBounds.X;
                properties[nameof(RectInt32.Y)] = settings.RestoreBounds.Y;
                properties[nameof(RectInt32.Width)] = settings.RestoreBounds.Width;
                properties[nameof(RectInt32.Height)] = settings.RestoreBounds.Height;
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        private static async Task SaveUnpackaged(Settings settings)
        {
            try
            {
                string path = GetSettingsFilePath();
                string? directory = Path.GetDirectoryName(path);
                Debug.Assert(!string.IsNullOrWhiteSpace(directory));

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, GetSerializerOptions()));
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        public static Settings Load()
        {
            if (App.IsPackaged)
                return LoadPackaged();

            return LoadUnpackaged();
        }

        private static Settings LoadPackaged()
        {
            Settings settings = new Settings();

            try
            {
                IPropertySet properties = ApplicationData.Current.LocalSettings.Values;

                settings.ChooseNumbersIndex = (int)properties[nameof(ChooseNumbersIndex)];
                settings.ChooseLettersIndex = (int)properties[nameof(ChooseLettersIndex)];
                settings.CurrentTheme = (ElementTheme)properties[nameof(CurrentTheme)];
                settings.WindowState = (WindowState)properties[nameof(WindowState)];

                settings.RestoreBounds = new RectInt32((int)properties[nameof(RectInt32.X)],
                                                    (int)properties[nameof(RectInt32.Y)],
                                                    (int)properties[nameof(RectInt32.Width)],
                                                    (int)properties[nameof(RectInt32.Height)]);
            }
            catch (Exception ex)
            {
                settings.IsFirstRun = true;
                Debug.WriteLine(ex.ToString());
            }

            return settings;
        }

        private static Settings LoadUnpackaged()
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



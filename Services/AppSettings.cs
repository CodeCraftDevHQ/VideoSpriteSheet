using System.Text.Json;
using System.Windows;

namespace VideoSpriteSheet.Services;

public sealed class AppSettings
{
    public string Language { get; set; } = "fa";

    public string Theme { get; set; } = "System";

    public string ModelsFolder { get; set; } = string.Empty;

    public static AppSettings Current { get; private set; } = new();

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VideoSpriteSheet",
        "settings.json");

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath));

                if (loaded is not null)
                {
                    Current = loaded;
                }
            }
        }
        catch
        {
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch
        {
        }
    }
}

public static class ThemeManager
{
    public static void Apply(string theme)
    {
        Application.Current.ThemeMode = theme switch
        {
            "Dark" => ThemeMode.Dark,
            "Light" => ThemeMode.Light,
            _ => ThemeMode.System
        };
    }
}

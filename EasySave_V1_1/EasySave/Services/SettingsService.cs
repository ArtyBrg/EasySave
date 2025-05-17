// Services/SettingsService.cs
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Models;

public static class SettingsService
{
    private const string SettingsPath = "settings.json";

    public static void Save(AppSettings settings)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(SettingsPath, json);
    }

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        var json = File.ReadAllText(SettingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        }) ?? new AppSettings();
    }
}

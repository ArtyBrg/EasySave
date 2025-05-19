// Services/SettingsService.cs
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Models;

// Static service for saving and loading application settings from/to a JSON file
public static class SettingsService
{
    // Path to the configuration file
    private const string SettingsPath = "settings.json";

    // Saves the application settings to a JSON file
    public static void Save(AppSettings settings)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // Readable formatting (indentation)
            Converters = { new JsonStringEnumConverter() }
        };

        // Serializes the AppSettings object to JSON
        var json = JsonSerializer.Serialize(settings, options);

        // Writes the JSON to the file
        File.WriteAllText(SettingsPath, json);
    }

    // Loads the settings from the JSON file
    public static AppSettings Load()
    {
        // Returns a new instance with default values if the file does not exist
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        var json = File.ReadAllText(SettingsPath);
        // Deserializes the JSON into an AppSettings object, or returns a default instance if it fails
        return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        }) ?? new AppSettings();
    }
}

// Services/SettingsService.cs
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Models;

namespace EasySave.Services
{
    // SettingsService class to handle loading and saving of settings
    public static class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, @"..\\..\\..\\", "Settings", "settings.json");

        // Ensure the settings directory exists
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

        // Load settings from the JSON file
        public static AppSettings Load()
        {
            // Check if the settings file exists, if not return default settings
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            }) ?? new AppSettings();
        }
    }
}
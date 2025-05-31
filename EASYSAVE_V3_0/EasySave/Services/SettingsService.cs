// Services/SettingsService.cs
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Models;
using EasySave.Services;

namespace EasySave.Services
{
    public static class SettingsService
    {
        // Path to the settings JSON file
        private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, @"..\\..\\..\\", "Settings", "settings.json");

        // Returns the list of priority file extensions from settings
        public static List<string> GetPriorityExtensions()
        {
            var settings = Load();
            return settings.PriorityExtensions ?? new List<string>();
        }

        // Saves the AppSettings object to the settings file in JSON format
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

        // Loads the AppSettings object from the settings file, or returns a new one if the file does not exist
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
}

using System;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text.Json;
using System.Threading;

namespace EasySave.Services
{
    public class LanguageService
    {
        private static ResourceManager _resourceManager = EASYSAVE_V2_0.Resources.Resources.ResourceManager;
        private static readonly string SettingsFilePath =
            Path.Combine(AppContext.BaseDirectory, "Settings.json");

        public void SetLanguage(string languageCode)
        {
            var culture = languageCode.ToUpper() switch
            {
                "EN" => new CultureInfo("en"),
                "FR" => new CultureInfo("fr"),
                _ => throw new ArgumentException("Invalid language. Please enter EN or FR.")
            };

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            SaveLanguageToSettings(languageCode); // Sauvegarde automatique
        }

        public void LoadLanguageFromSettings()
        {
            if (!File.Exists(SettingsFilePath))
            {
                return;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<Settings>(json);

            if (!string.IsNullOrEmpty(settings?.Language))
            {
                SetLanguage(settings.Language);
            }
        }

        public string GetString(string key)
        {
            return _resourceManager.GetString(key) ?? $"[{key}]";
        }

        private void SaveLanguageToSettings(string languageCode)
        {
            var settings = new Settings { Language = languageCode.ToUpper() };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }

        private class Settings
        {
            public string Language { get; set; }
        }
    }
}
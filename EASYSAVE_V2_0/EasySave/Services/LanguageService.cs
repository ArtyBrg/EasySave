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

            var settings = SettingsService.Load();
            settings.Language = languageCode.ToUpper();
            SettingsService.Save(settings);
        }

        public void LoadLanguageFromSettings()
        {
            var settings = SettingsService.Load();

            if (!string.IsNullOrEmpty(settings.Language))
            {
                SetLanguage(settings.Language);
            }
            else
            {
                // Si aucune langue n'est définie, on utilise la langue par défaut (anglais)
            }
        }

        public string GetString(string key)
        {
            return _resourceManager.GetString(key) ?? $"[{key}]";
        }

        private class Settings
        {
            public string Language { get; set; }
        }
    }
}
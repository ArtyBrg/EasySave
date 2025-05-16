using System;
using System.Globalization;
using System.Resources;
using System.Threading;
using EasySave.Models;

namespace EasySave.Services
{
    public class LanguageService
    {
        private static ResourceManager _resourceManager = EasySave_V1_1.Resources.Resources.ResourceManager;

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
                Console.WriteLine("No language found in settings. Using default.");
            }
        }

        public string GetString(string key)
        {
            return _resourceManager.GetString(key) ?? $"[{key}]";
        }
    }
}

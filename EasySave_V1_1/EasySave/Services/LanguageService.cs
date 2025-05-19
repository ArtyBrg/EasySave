//Services/LanguageService.cs
using System;
using System.Globalization;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using EasySave.Models;
using EasySave_V1_1.Resources;

namespace EasySave.Services
{
    //Changing languages (English and French) and loading localised resources
    public class LanguageService
    {
        // ResourceManager used to access localised channels (EN/FR)
        private static ResourceManager _resourceManager = EasySave_V1_1.Resources.Resources.ResourceManager;

        //Defines the application language
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

        //Automatically loads the language saved in the configuration settings
        public void LoadLanguageFromSettings()
        {
            var settings = SettingsService.Load();

            if (!string.IsNullOrEmpty(settings.Language))
            {
                SetLanguage(settings.Language); // Apply the saved language
            }
            else
            {
                Console.WriteLine("No language found in settings. Using default.");
            }
        }

        //Recovering a localised string from resource files using a key
        public string GetString(string key)
        {
            return _resourceManager.GetString(key) ?? $"[{key}]";
        }
    }
}

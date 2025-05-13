using System;
using System.Globalization;
using System.Resources;
using System.Threading;

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
        }

        public string GetString(string key)
        {
            return _resourceManager.GetString(key) ?? $"[{key}]";
        }
    }
}
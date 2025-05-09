using System;

// Gestionnaire de langue

namespace EasySave
{
    public class LanguageManager
    {
        private string _currentLanguage = "EN";

        public void SetLanguage(string language)
        {
            _currentLanguage = language.ToUpper() switch
            {
                "FR" => "FR",
                "EN" => "EN",
                _ => throw new ArgumentException("Unsupported language")
            };
            Logger.Log($"Language set to: {_currentLanguage}");
        }

        public string GetCurrentLanguage() => _currentLanguage;
    }
}

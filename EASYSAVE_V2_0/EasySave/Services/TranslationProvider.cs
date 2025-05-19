using System.ComponentModel;
using EasySave.Services;

namespace EasySave.Services
{
    // Class to manage the language settings of the application
    public class TranslationProvider : INotifyPropertyChanged
    {
        private readonly LanguageService _languageService;

        public TranslationProvider(LanguageService languageService)
        {
            _languageService = languageService;
        }

        // Set the language for the application
        public string this[string key] => _languageService.GetString(key);

        // Load the language from the settings file
        public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

        // Get the current language
        public event PropertyChangedEventHandler PropertyChanged;
    }
}

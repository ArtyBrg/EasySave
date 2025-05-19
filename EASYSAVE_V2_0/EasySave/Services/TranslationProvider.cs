using System.ComponentModel;
using EasySave.Services;

namespace EasySave.Services
{
    public class TranslationProvider : INotifyPropertyChanged
    {
        private readonly LanguageService _languageService;

        public TranslationProvider(LanguageService languageService)
        {
            _languageService = languageService;
        }

        public string this[string key] => _languageService.GetString(key);

        public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

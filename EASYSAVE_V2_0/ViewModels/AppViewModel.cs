using EasySave.Services;
using System.ComponentModel;
using System.Windows.Input;
using LoggerLib;
using System.Reflection.Metadata;
using System.Text;
using EasySave_WPF;

namespace EasySave.ViewModels
{
    public class AppViewModel : ViewModelBase
    {
        private readonly LanguageService _languageService;
        private readonly LoggerService _loggerService;

        public TranslationProvider Translations { get; }

        private StringBuilder _logBuilder = new StringBuilder();
        private string _logContent = string.Empty;

        private string _errorMessage;
        private string _currentLanguage;

        public AppViewModel()
        {
            _languageService = new LanguageService();
            _loggerService = new LoggerService();
            _languageService.LoadLanguageFromSettings();

            Translations = new TranslationProvider(_languageService);

            _loggerService.LogMessageAdded += OnLogMessageAdded;
            _loggerService.Log("AppViewModel initialized - Log system ready");
        }
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }

        private void OnLogMessageAdded(object sender, string message)
        {
            // Ajouter le message à notre builder
            _logBuilder.AppendLine(message);
            // Mettre à jour la propriété LogContent avec le contenu complet
            LogContent = _logBuilder.ToString();
        }

        public void ChangeLanguages(string language)
        {
            if (string.IsNullOrEmpty(language))
                return;

            try
            {
                _languageService.SetLanguage(language);
                CurrentLanguage = language.ToUpper();
                _loggerService.Log($"Language changed to {language}");

                Translations.Refresh();

            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error changing language: {ex.Message}";
                _loggerService.LogError($"Failed to change language: {ex.Message}");
            }
        }
    }
}
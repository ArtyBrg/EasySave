using EasySave.Services;
using System.ComponentModel;
using System.Windows.Input;
using LoggerLib;
using System.Reflection.Metadata;
using System.Text;
using EasySave_WPF;
using System.Collections.ObjectModel;

namespace EasySave.ViewModels
{
    // ViewModel for the main application
    public class AppViewModel : ViewModelBase
    {
        private readonly LanguageService _languageService;
        private readonly LoggerService _loggerService;

        public TranslationProvider Translations { get; }

        private StringBuilder _logBuilder = new StringBuilder();
        private string _logContent = string.Empty;

        private string _errorMessage;
        private string _currentLanguage;

        public ObservableCollection<BackupJobViewModel> ActiveBackupJobs { get; } = new ObservableCollection<BackupJobViewModel>();


        // Constructor for the AppViewModel
        public AppViewModel()
        {
            _languageService = new LanguageService();
            _loggerService = new LoggerService();
            _languageService.LoadLanguageFromSettings();

            Translations = new TranslationProvider(_languageService);

            _loggerService.LogMessageAdded += OnLogMessageAdded;
            _loggerService.Log("AppViewModel initialized - Log system ready");
        }
        // Property to bind the log content to the UI
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }
        // Property to bind the error message to the UI
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }
        // Property to bind the current language to the UI
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }
        // Property to bind the translation provider to the UI
        private void OnLogMessageAdded(object sender, string message)
        {
            // Add the new log message to the StringBuilder
            _logBuilder.AppendLine(message);
            // Update the log content property
            LogContent = _logBuilder.ToString();
        }

        // Command to change the language
        public void ChangeLanguages(string language)
        {
            // Check if the language is valid
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
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
        // Service for managing application language
        private readonly LanguageService _languageService;
        // Service for logging messages and errors
        private readonly LoggerService _loggerService;

        // Provides translations for the UI
        public TranslationProvider Translations { get; }

        // StringBuilder to accumulate log messages
        private StringBuilder _logBuilder = new StringBuilder();
        // Stores the current log content as a string
        private string _logContent = string.Empty;

        // Stores the current error message
        private string _errorMessage;
        // Stores the current language code
        private string _currentLanguage;

        // Collection of active backup jobs for the UI
        public ObservableCollection<BackupJobViewModel> ActiveBackupJobs { get; } = new ObservableCollection<BackupJobViewModel>();

        // Constructor for the AppViewModel
        public AppViewModel()
        {
            _languageService = new LanguageService();
            _loggerService = new LoggerService();
            _languageService.LoadLanguageFromSettings();

            Translations = new TranslationProvider(_languageService);

            // Subscribe to log message events
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

        // Handles new log messages and updates the log content
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
                // Set the new language in the language service
                _languageService.SetLanguage(language);
                // Update the current language property
                CurrentLanguage = language.ToUpper();
                // Log the language change
                _loggerService.Log($"Language changed to {language}");

                // Refresh translations for the UI
                Translations.Refresh();
            }
            catch (Exception ex)
            {
                // Set and log the error message
                ErrorMessage = $"Error changing language: {ex.Message}";
                _loggerService.LogError($"Failed to change language: {ex.Message}");
            }
        }
    }
}

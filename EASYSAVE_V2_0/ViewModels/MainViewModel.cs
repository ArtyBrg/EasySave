using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using EasySave.Services;
using LoggerLib;
using System.Text;
using EasySave.Models;

namespace EasySave.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly BackupManagerViewModel _backupManager;
        private readonly LanguageService _languageService;
        private readonly LoggerService _loggerService;

        private string _selectedViewName = "Home";
        private string _logContent = "";
        private string _currentLanguage = "EN";
        private string _newJobName = "";
        private string _newJobSourcePath = "";
        private string _newJobTargetPath = "";
        private string _newJobType = "Complete";
        private string _errorMessage = "";
        private StringBuilder _logBuilder = new StringBuilder();
        private string _selectedLogFormat = "JSON";

        public MainViewModel(
            BackupManagerViewModel backupManager,
            LanguageService languageService,
            LoggerService loggerService)
        {
            _backupManager = backupManager;
            _languageService = languageService;
            _loggerService = loggerService;

            _loggerService.LogMessageAdded += (sender, message) =>
                LogContent += message + Environment.NewLine;

            // Initialisation
            _currentLanguage = "EN"; // Valeur par défaut
            _selectedViewName = "Home";
            _newJobType = "Complete";

            // Abonnement aux événements
            _loggerService.LogMessageAdded += OnLogMessageAdded;
            _loggerService.Log("MainViewModel initialized - Log system ready");

            NavigateCommand = new RelayCommand(Navigate);
            SetLanguageCommand = new RelayCommand(SetLanguage);
            CreateJobCommand = new RelayCommand(CreateBackupJob, CanCreateJob);
            BrowseSourceCommand = new RelayCommand(BrowseSourcePath);
            BrowseTargetCommand = new RelayCommand(BrowseTargetPath);
        }

        public BackupManagerViewModel BackupManager => _backupManager;

        private void OnLogMessageAdded(object sender, string message)
        {
            // Ajouter le message à notre builder
            _logBuilder.AppendLine(message);
            // Mettre à jour la propriété LogContent avec le contenu complet
            LogContent = _logBuilder.ToString();
        }

        public string SelectedViewName
        {
            get => _selectedViewName;
            set => SetProperty(ref _selectedViewName, value);
        }

        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }

        public string NewJobName
        {
            get => _newJobName;
            set
            {
                SetProperty(ref _newJobName, value);
                ((RelayCommand)CreateJobCommand).RaiseCanExecuteChanged();
            }
        }

        public string NewJobSourcePath
        {
            get => _newJobSourcePath;
            set
            {
                SetProperty(ref _newJobSourcePath, value);
                ((RelayCommand)CreateJobCommand).RaiseCanExecuteChanged();
            }
        }

        public string NewJobTargetPath
        {
            get => _newJobTargetPath;
            set
            {
                SetProperty(ref _newJobTargetPath, value);
                ((RelayCommand)CreateJobCommand).RaiseCanExecuteChanged();
            }
        }

        public string NewJobType
        {
            get => _newJobType;
            set => SetProperty(ref _newJobType, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand SetLanguageCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseTargetCommand { get; }

        public string SelectedLogFormat
        {
            get => _selectedLogFormat;
            set
            {
                if (SetProperty(ref _selectedLogFormat, value))
                {
                    LogFormat format = value.ToUpper() switch
                    {
                        "XML" => LogFormat.Xml,
                        _ => LogFormat.Json
                    };
                    _loggerService.SetLogFormat(format);
                    _loggerService.Log($"Log format changed to {value.ToUpper()}");
                }
            }
        }

        private void Navigate(object parameter)
        {
            if (parameter is string viewName)
            {
                _loggerService.Log($"Navigating to view: {viewName}");
                SelectedViewName = viewName;
                ErrorMessage = string.Empty;
            }
        }

        private void SetLanguage(object parameter)
        {
            if (parameter is not string language) return;

            try
            {
                _languageService.SetLanguage(language);
                CurrentLanguage = language.ToUpper();
                _loggerService.Log($"Language changed to {language}");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error changing language: {ex.Message}";
                _loggerService.LogError($"Failed to change language: {ex.Message}");
            }
        }

        private void CreateBackupJob(object _)
        {
            ErrorMessage = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(NewJobName) ||
                    string.IsNullOrWhiteSpace(NewJobSourcePath) ||
                    string.IsNullOrWhiteSpace(NewJobTargetPath))
                {
                    ErrorMessage = "All fields must be filled.";
                    return;
                }

                string[] jobParams = { NewJobName, NewJobSourcePath, NewJobTargetPath, NewJobType };
                _backupManager.CreateJob(jobParams);

                _loggerService.Log($"Created new backup job: {NewJobName} ({NewJobType})");

                NewJobName = string.Empty;
                NewJobSourcePath = string.Empty;
                NewJobTargetPath = string.Empty;
                NewJobType = "Complete";
                SelectedViewName = "JobsList";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating job: {ex.Message}";
                _loggerService.LogError($"Exception creating job: {ex}");
            }
        }

        private bool CanCreateJob(object _)
        {
            return !string.IsNullOrWhiteSpace(NewJobName) &&
                   !string.IsNullOrWhiteSpace(NewJobSourcePath) &&
                   !string.IsNullOrWhiteSpace(NewJobTargetPath) &&
                   _backupManager.CanCreateJob(null);
        }

        private void BrowseSourcePath(object _)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                NewJobSourcePath = dialog.SelectedPath;
                _loggerService.Log($"Source path selected: {dialog.SelectedPath}");
            }
        }

        private void BrowseTargetPath(object _)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                NewJobTargetPath = dialog.SelectedPath;
                _loggerService.Log($"Target path selected: {dialog.SelectedPath}");
            }
        }

        public string GetLocalizedString(string key) => _languageService.GetString(key);
    }
}
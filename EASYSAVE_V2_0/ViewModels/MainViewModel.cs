using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using EasySave.Services;

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

            NavigateCommand = new RelayCommand(Navigate);
            SetLanguageCommand = new RelayCommand(SetLanguage);
            CreateJobCommand = new RelayCommand(CreateBackupJob, CanCreateJob);
            BrowseSourceCommand = new RelayCommand(BrowseSourcePath);
            BrowseTargetCommand = new RelayCommand(BrowseTargetPath);
        }

        public BackupManagerViewModel BackupManager => _backupManager;

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

        private void Navigate(object parameter)
        {
            if (parameter is string viewName)
            {
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
        _backupManager.CreateJob(jobParams); // Ne plus attendre de retour

        NewJobName = string.Empty;
        NewJobSourcePath = string.Empty;
        NewJobTargetPath = string.Empty;
        NewJobType = "Complete";
        SelectedViewName = "JobsList";
    }
    catch (Exception ex)
    {
        ErrorMessage = $"Error creating job: {ex.Message}";
        _loggerService.LogError($"Exception: {ex}");
    }
}

        private bool CanCreateJob(object _)
        {
            return !string.IsNullOrWhiteSpace(NewJobName) &&
                   !string.IsNullOrWhiteSpace(NewJobSourcePath) &&
                   !string.IsNullOrWhiteSpace(NewJobTargetPath) &&
                   _backupManager.CanCreateJob(_);
        }

        private void BrowseSourcePath(object _)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                NewJobSourcePath = dialog.SelectedPath;
            }
        }

        private void BrowseTargetPath(object _)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                NewJobTargetPath = dialog.SelectedPath;
            }
        }

        public string GetLocalizedString(string key) => _languageService.GetString(key);
    }
}
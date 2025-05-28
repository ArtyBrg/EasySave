using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using EasySave.Services;
using System.Windows.Forms;
using LoggerLib;
using System.Text;
using EasySave.Models;
using System.ComponentModel;
using EasySave_WPF;
using System.Runtime.InteropServices;

namespace EasySave.ViewModels
{
    // MainViewModel is the main view model for the application.
    public class MainViewModel : ViewModelBase
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        // Services
        private readonly BackupManagerViewModel _backupManagerViewModel;
        private readonly LanguageService _languageService;
        private readonly LoggerService _loggerService;
        private readonly RemoteConsoleService _remoteConsole;

        // Properties
        private string _selectedViewName;
        private StringBuilder _logBuilder = new StringBuilder();
        private string _logContent = string.Empty;

        // Properties for creating a new job
        private string _newJobName;
        private string _newJobSourcePath;
        private string _newJobTargetPath;
        private string _newJobType;
        private string _errorMessage;

        // Properties for language management
        private string _currentLanguage = null;

        // Constructor
        public MainViewModel(
            BackupManagerViewModel backupManagerViewModel,
            LanguageService languageService,
            LoggerService loggerService)
        {

            // AllocConsole();
            _backupManagerViewModel = backupManagerViewModel;
            _languageService = languageService;
            _loggerService = loggerService;

            _remoteConsole = new RemoteConsoleService(StateService.Instance);

            // Passe le vrai ViewModel une fois ici :
            _remoteConsole.Initialize(_backupManagerViewModel);
            StateService.Instance.SetRemoteConsole(_remoteConsole);
            _remoteConsole.Start();
            InitLanguage();

            // Initialization
            _selectedViewName = "Home";
            _newJobType = "Complete";

            // Events subscription
            _loggerService.LogMessageAdded += OnLogMessageAdded;
            _loggerService.Log("MainViewModel initialized - Log system ready");

            // Commands
            NavigateCommand = new RelayCommand(Navigate);
            CreateJobCommand = new RelayCommand(CreateBackupJob, CanCreateJob);
            BrowseSourceCommand = new RelayCommand(param => BrowseSourcePath());
            BrowseTargetCommand = new RelayCommand(param => BrowseTargetPath());
            SetLanguageCommand = new RelayCommand(param => ChangeLanguage(param as string));
        }

        // Change the language of the application
        private void OnLogMessageAdded(object sender, string message)
        {
            // Add the new log message to the StringBuilder
            _logBuilder.AppendLine(message);
            // Update the log content property
            LogContent = _logBuilder.ToString();
        }

        // Change the language of the application
        public BackupManagerViewModel BackupManager => _backupManagerViewModel;
        // Property to get the list of backup jobs
        public string SelectedViewName
        {
            get => _selectedViewName;
            set => SetProperty(ref _selectedViewName, value);
        }

        // Property to get the list of backup jobs
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        // Properties for creating a new job
        public string NewJobName
        {
            get => _newJobName;
            set
            {
                SetProperty(ref _newJobName, value);
                ((RelayCommand)CreateJobCommand).RaiseCanExecuteChanged();
            }
        }

        // Property for the source path of the new job
        public string NewJobSourcePath
        {
            get => _newJobSourcePath;
            set
            {
                SetProperty(ref _newJobSourcePath, value);
                ((RelayCommand)CreateJobCommand).RaiseCanExecuteChanged();
            }
        }

        // Property for the target path of the new job
        public string NewJobTargetPath
        {
            get => _newJobTargetPath;
            set
            {
                SetProperty(ref _newJobTargetPath, value);
                ((RelayCommand)CreateJobCommand).RaiseCanExecuteChanged();
            }
        }

        // Property for the type of the new job
        public string NewJobType
        {
            get => _newJobType;
            set => SetProperty(ref _newJobType, value);
        }

        // Property for the error message
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // Property for the current language
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged();
                }
            }
        }
        // Property for the list of available languages
        public ICommand NavigateCommand { get; }
        public ICommand SetLanguageCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseTargetCommand { get; }

        private string _selectedLogFormat = "JSON";
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

        // Change the language of the application
        private void InitLanguage()
        {
            var settings = SettingsService.Load();
            CurrentLanguage = settings?.Language ?? "FR";

            // Enforce the language setting
            OnPropertyChanged(nameof(CurrentLanguage));
        }

        private void CreateBackupJob(object parameter)
        {
            ErrorMessage = string.Empty;

            try
            {
                // Validate the input fields
                if (string.IsNullOrWhiteSpace(NewJobName) ||
                    string.IsNullOrWhiteSpace(NewJobSourcePath) ||
                    string.IsNullOrWhiteSpace(NewJobTargetPath))
                {
                    ErrorMessage = "All fields must be filled.";
                    return;
                }

                // Check if the job name already exists
                if (_backupManagerViewModel.JobNameExists(NewJobName))
                {
                    ErrorMessage = "A job with this name already exists";
                    return;
                }

                string[] jobParams = { NewJobName, NewJobSourcePath, NewJobTargetPath, NewJobType };
                var job = _backupManagerViewModel.CreateJob(jobParams);

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

        // Check if the job can be created
        private bool CanCreateJob(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewJobName) &&
                   !string.IsNullOrWhiteSpace(NewJobSourcePath) &&
                   !string.IsNullOrWhiteSpace(NewJobTargetPath);
        }


        // Selectipon of the source path
        private void BrowseSourcePath()
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                NewJobSourcePath = dialog.SelectedPath;
                _loggerService.Log($"Source path selected: {dialog.SelectedPath}");
            }
        }

        // Selection of the target path
        private void BrowseTargetPath()
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                NewJobTargetPath = dialog.SelectedPath;
                _loggerService.Log($"Target path selected: {dialog.SelectedPath}");
            }
        }

        // Methods for the language management
        public string GetLocalizedString(string key)
        {
            return _languageService.GetString(key);
        }
    }
}
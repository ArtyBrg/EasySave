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
        // Import for allocating a console window (for debugging)
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        // Services used by the main view model
        private readonly BackupManagerViewModel _backupManagerViewModel;
        private readonly LanguageService _languageService;
        private readonly LoggerService _loggerService;
        private readonly RemoteConsoleService _remoteConsole;

        // Name of the currently selected view
        private string _selectedViewName;
        // StringBuilder for accumulating log messages
        private StringBuilder _logBuilder = new StringBuilder();
        // Stores the current log content as a string
        private string _logContent = string.Empty;

        // Fields for creating a new backup job
        private string _newJobName;
        private string _newJobSourcePath;
        private string _newJobTargetPath;
        private string _newJobType;
        private string _errorMessage;

        // Field for the current language
        private string _currentLanguage = null;

        // Constructor initializes services, remote console, and commands
        public MainViewModel(
            BackupManagerViewModel backupManagerViewModel,
            LanguageService languageService,
            LoggerService loggerService)
        {
            // AllocConsole(); // Uncomment to show a console window for debugging
            _backupManagerViewModel = backupManagerViewModel;
            _languageService = languageService;
            _loggerService = loggerService;

            // Initialize remote console and connect it to the backup manager
            _remoteConsole = new RemoteConsoleService(StateService.Instance);
            _remoteConsole.Initialize(_backupManagerViewModel);
            StateService.Instance.SetRemoteConsole(_remoteConsole);
            _remoteConsole.Start();
            InitLanguage();

            // Set default view and job type
            _selectedViewName = "Home";
            _newJobType = "Complete";

            // Subscribe to log message events
            _loggerService.LogMessageAdded += OnLogMessageAdded;
            _loggerService.Log("MainViewModel initialized - Log system ready");

            // Initialize commands for UI actions
            NavigateCommand = new RelayCommand(Navigate);
            CreateJobCommand = new RelayCommand(CreateBackupJob, CanCreateJob);
            BrowseSourceCommand = new RelayCommand(param => BrowseSourcePath());
            BrowseTargetCommand = new RelayCommand(param => BrowseTargetPath());
            SetLanguageCommand = new RelayCommand(param => ChangeLanguage(param as string));
        }

        // Handles new log messages and updates the log content
        private void OnLogMessageAdded(object sender, string message)
        {
            // Add the new log message to the StringBuilder
            _logBuilder.AppendLine(message);
            // Update the log content property
            LogContent = _logBuilder.ToString();
        }

        // Exposes the backup manager view model
        public BackupManagerViewModel BackupManager => _backupManagerViewModel;

        // Name of the currently selected view (for navigation)
        public string SelectedViewName
        {
            get => _selectedViewName;
            set => SetProperty(ref _selectedViewName, value);
        }

        // Log content to display in the UI
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        // Name for the new backup job
        public string NewJobName
        {
            get => _newJobName;
            set
            {
                SetProperty(ref _newJobName, value);
                ((RelayCommand)CreateJobCommand).RaiseCanExecuteChanged();
            }
        }

        // Source path for the new backup job
        public string NewJobSourcePath
        {
            get => _newJobSourcePath;
            set
            {
                SetProperty(ref _newJobSourcePath, value);
                ((RelayCommand)CreateJobCommand).RaiseCanExecuteChanged();
            }
        }

        // Target path for the new backup job
        public string NewJobTargetPath
        {
            get => _newJobTargetPath;
            set
            {
                SetProperty(ref _newJobTargetPath, value);
                ((RelayCommand)CreateJobCommand).RaiseCanExecuteChanged();
            }
        }

        // Type for the new backup job (e.g., Complete or Differential)
        public string NewJobType
        {
            get => _newJobType;
            set => SetProperty(ref _newJobType, value);
        }

        // Error message to display in the UI
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // Current language code
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

        // Commands for navigation, language, job creation, and browsing
        public ICommand NavigateCommand { get; }
        public ICommand SetLanguageCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseTargetCommand { get; }

        // Selected log format (JSON or XML)
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

        // Handles navigation between views
        private void Navigate(object parameter)
        {
            if (parameter is string viewName)
            {
                _loggerService.Log($"Navigating to view: {viewName}");
                SelectedViewName = viewName;
                ErrorMessage = string.Empty;
            }
        }

        // Initializes the language from settings
        private void InitLanguage()
        {
            var settings = SettingsService.Load();
            CurrentLanguage = settings?.Language ?? "FR";
            // Notify UI of language change
            OnPropertyChanged(nameof(CurrentLanguage));
        }

        // Creates a new backup job using the input fields
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

                // Reset input fields after creation
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

        // Determines if a new job can be created (all fields must be filled)
        private bool CanCreateJob(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewJobName) &&
                   !string.IsNullOrWhiteSpace(NewJobSourcePath) &&
                   !string.IsNullOrWhiteSpace(NewJobTargetPath);
        }

        // Opens a folder browser dialog to select the source path
        private void BrowseSourcePath()
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                NewJobSourcePath = dialog.SelectedPath;
                _loggerService.Log($"Source path selected: {dialog.SelectedPath}");
            }
        }

        // Opens a folder browser dialog to select the target path
        private void BrowseTargetPath()
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                NewJobTargetPath = dialog.SelectedPath;
                _loggerService.Log($"Target path selected: {dialog.SelectedPath}");
            }
        }

        // Gets a localized string for the given key
        public string GetLocalizedString(string key)
        {
            return _languageService.GetString(key);
        }
    }
}

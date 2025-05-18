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

namespace EasySave.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly BackupManagerViewModel _backupManagerViewModel;
        private readonly LanguageService _languageService;
        private readonly LoggerService _loggerService;

        private string _selectedViewName;
        private StringBuilder _logBuilder = new StringBuilder();
        private string _logContent = string.Empty;

        // Propriétés pour la création de jobs
        private string _newJobName;
        private string _newJobSourcePath;
        private string _newJobTargetPath;
        private string _newJobType;
        private string _errorMessage;

        public MainViewModel(
            BackupManagerViewModel backupManagerViewModel,
            LanguageService languageService,
            LoggerService loggerService)
        {
            _backupManagerViewModel = backupManagerViewModel;
            _languageService = languageService;
            _loggerService = loggerService;

            InitLanguage();

            // Initialisation
            _selectedViewName = "Home";
            _newJobType = "Complete";

            // Abonnement aux événements
            _loggerService.LogMessageAdded += OnLogMessageAdded;
            _loggerService.Log("MainViewModel initialized - Log system ready");

            // Commandes
            NavigateCommand = new RelayCommand(Navigate);
            CreateJobCommand = new RelayCommand(CreateBackupJob, CanCreateJob);
            BrowseSourceCommand = new RelayCommand(param => BrowseSourcePath());
            BrowseTargetCommand = new RelayCommand(param => BrowseTargetPath());
            SetLanguageCommand = new RelayCommand(param => ChangeLanguage(param as string));
        }

        public BackupManagerViewModel BackupManager => _backupManager;

        private void OnLogMessageAdded(object sender, string message)
        {
            // Ajouter le message à notre builder
            _logBuilder.AppendLine(message);
            // Mettre à jour la propriété LogContent avec le contenu complet
            LogContent = _logBuilder.ToString();
        }

        public BackupManagerViewModel BackupManager => _backupManagerViewModel;
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
        // Commandespublic ICommand NavigateCommand { get; }
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

        private void InitLanguage()
        {
            var settings = SettingsService.Load();
            CurrentLanguage = settings?.Language ?? "FR";

            // Forcer la notification pour la couleur au démarrage
            OnPropertyChanged(nameof(CurrentLanguage));
        }

        private void CreateBackupJob(object parameter)
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

        private bool CanCreateJob(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewJobName) &&
                   !string.IsNullOrWhiteSpace(NewJobSourcePath) &&
                   !string.IsNullOrWhiteSpace(NewJobTargetPath);
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

        // Méthodes auxiliaires pour la traduction
        public string GetLocalizedString(string key)
        {
            return _languageService.GetString(key);
        }
    }
}
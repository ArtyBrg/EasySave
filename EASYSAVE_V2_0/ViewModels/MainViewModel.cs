using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using EasySave.Services;

namespace EasySave.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly BackupManagerViewModel _backupManagerViewModel;
        private readonly LanguageService _languageService;
        private readonly LoggerService _loggerService;

        private string _selectedViewName;
        private string _logContent;
        private string _currentLanguage;

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

            // Initialisation
            _currentLanguage = "EN"; // Valeur par défaut
            _selectedViewName = "Home";
            _newJobType = "Complete";

            // Abonnement aux événements
            _loggerService.LogMessageAdded += (sender, message) =>
            {
                LogContent += message + Environment.NewLine;
            };

            // Commandes
            NavigateCommand = new RelayCommand(Navigate);
            SetLanguageCommand = new RelayCommand(param => SetLanguage(param as string));
            CreateJobCommand = new RelayCommand(CreateBackupJob, CanCreateJob);
            BrowseSourceCommand = new RelayCommand(param => BrowseSourcePath());
            BrowseTargetCommand = new RelayCommand(param => BrowseTargetPath());
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

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }

        // Propriétés pour la création de jobs
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

        // Commandes
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
                ErrorMessage = string.Empty; // Réinitialiser les messages d'erreur lors de la navigation
            }
        }

        private void SetLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
                return;

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

        private void CreateBackupJob(object parameter)
        {
            ErrorMessage = string.Empty;

            try
            {
                if (_backupManagerViewModel.JobNameExists(NewJobName))
                {
                    ErrorMessage = "A job with this name already exists";
                    return;
                }

                string[] jobParams = { NewJobName, NewJobSourcePath, NewJobTargetPath, NewJobType };
                var job = _backupManagerViewModel.CreateJob(jobParams);

                // Réinitialiser les champs après création
                NewJobName = string.Empty;
                NewJobSourcePath = string.Empty;
                NewJobTargetPath = string.Empty;
                NewJobType = "Complete";

                // Navigation optionnelle vers la liste des jobs
                SelectedViewName = "JobsList";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error creating job: {ex.Message}";
                _loggerService.LogError($"Failed to create job: {ex.Message}");
            }
        }

        private bool CanCreateJob(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewJobName) &&
                   !string.IsNullOrWhiteSpace(NewJobSourcePath) &&
                   !string.IsNullOrWhiteSpace(NewJobTargetPath) &&
                   _backupManagerViewModel.CanCreateJob();
        }

        private void BrowseSourcePath()
        {
            // Utiliser une méthode déléguée pour permettre l'interaction avec Windows Forms
            // Cette méthode sera appelée depuis la vue via le Command Binding
            // La vue devra gérer l'affichage du FolderBrowserDialog
            NewJobSourcePath = "C:\\Temp"; // Simulation - à remplacer par l'intégration réelle
        }

        private void BrowseTargetPath()
        {
            // Comme pour BrowseSourcePath
            NewJobTargetPath = "D:\\Backup"; // Simulation - à remplacer par l'intégration réelle  
        }

        // Méthodes auxiliaires pour la traduction
        public string GetLocalizedString(string key)
        {
            return _languageService.GetString(key);
        }
    }
}
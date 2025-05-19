using System;
using System.Collections.Generic;
using System.Linq;
using EasySave.Services;

namespace EasySave.ViewModels
{
    // ViewModel for the main application
    public class MainViewModel
    {
        // ViewModel for managing backup jobs
        private readonly BackupManagerViewModel _backupManagerViewModel;
        // Service for managing language translations
        private readonly LanguageService _languageService;

        // Constructor that initializes the backup manager and language services
        public MainViewModel(BackupManagerViewModel backupManagerViewModel, LanguageService languageService)
        {
            _backupManagerViewModel = backupManagerViewModel;
            _languageService = languageService;
        }

        // Properties to access the backup manager and language services
        public void SetLanguage(string language)
        {
            _languageService.SetLanguage(language);
        }

        // Method to load the language from settings
        public string GetTranslation(string key)
        {
            return _languageService.GetString(key);
        }

        // Method to create a new backup job
        public BackupJobViewModel CreateBackupJob(string name, string source, string target, string type)
        {
            return _backupManagerViewModel.CreateJob(name, source, target, type);
        }

        // Method to delete a backup job by ID
        public void ExecuteBackupJobs(IEnumerable<int> jobIds)
        {
            _backupManagerViewModel.ExecuteJobs(jobIds);
        }

        // Method to delete a backup job by ID
        public IEnumerable<BackupJobViewModel> GetAllJobs()
        {
            return _backupManagerViewModel.GetAllJobs();
        }


        // Method to delete a backup job by ID
        public bool JobNameExists(string name)
        {
            return _backupManagerViewModel.GetAllJobs().Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
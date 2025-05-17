using System;
using System.Collections.Generic;
using System.Linq;
using EasySave.Services;

namespace EasySave.ViewModels
{
    public class MainViewModel
    {
        private readonly BackupManagerViewModel _backupManagerViewModel;
        private readonly LanguageService _languageService;

        public MainViewModel(BackupManagerViewModel backupManagerViewModel, LanguageService languageService)
        {
            _backupManagerViewModel = backupManagerViewModel;
            _languageService = languageService;
        }

        public void SetLanguage(string language)
        {
            _languageService.SetLanguage(language);
        }

        public string GetTranslation(string key)
        {
            return _languageService.GetString(key);
        }

        public BackupJobViewModel CreateBackupJob(string name, string source, string target, string type)
        {
            return _backupManagerViewModel.CreateJob(name, source, target, type);
        }

        public void ExecuteBackupJobs(IEnumerable<int> jobIds)
        {
            _backupManagerViewModel.ExecuteJobs(jobIds);
        }

        public IEnumerable<BackupJobViewModel> GetAllJobs()
        {
            return _backupManagerViewModel.GetAllJobs();
        }


        public bool JobNameExists(string name)
        {
            return _backupManagerViewModel.GetAllJobs().Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
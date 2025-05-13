using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasySave.Models;
using EasySave.Services;

namespace EasySave.ViewModels
{
    public class BackupManagerViewModel
    {
        private readonly List<BackupJobViewModel> _jobs = new();
        private int _nextId = 1;
        private readonly FileSystemService _fileSystemService;
        private readonly LoggerService _loggerService;
        private readonly StateService _stateService;

        public BackupManagerViewModel(FileSystemService fileSystemService,
                                     LoggerService loggerService,
                                     StateService stateService)
        {
            _fileSystemService = fileSystemService;
            _loggerService = loggerService;
            _stateService = stateService;
        }

        public BackupJobViewModel CreateJob(string name, string source, string target, string type)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Job name cannot be null or empty", nameof(name));

            var job = new BackupJob
            {
                Id = _nextId++,
                Name = name,
                SourcePath = Path.GetFullPath(source),
                TargetPath = Path.GetFullPath(target),
                Type = type
            };

            var jobViewModel = new BackupJobViewModel(job, _fileSystemService, _loggerService, _stateService);
            _jobs.Add(jobViewModel);
            _stateService.UpdateState(name, "Created", 0);
            _loggerService.Log($"Created new backup job: {name} (ID: {jobViewModel.Id})");

            return jobViewModel;
        }

        public void ExecuteJobs(IEnumerable<int> jobIds)
        {
            foreach (var id in jobIds)
            {
                var job = _jobs.FirstOrDefault(j => j.Id == id);
                if (job != null)
                {
                    try
                    {
                        job.Execute();
                    }
                    catch (Exception ex)
                    {
                        _loggerService.LogError($"Failed to execute backup job {job.Name}: {ex.Message}");
                    }
                }
            }
        }

        public IEnumerable<BackupJobViewModel> GetAllJobs() => _jobs.AsReadOnly();
    }
}
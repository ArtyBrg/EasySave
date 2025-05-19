using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EasySave.Models;
using EasySave.Services;

namespace EasySave.ViewModels
{
    // ViewModel for managing backup jobs
    public class BackupManagerViewModel
    {
        // List of backup jobs
        private readonly List<BackupJobViewModel> _jobs = new();
        // Next available ID for a new job
        private int _nextId = 1;
        // Services used for file system operations, logging, and state management
        private readonly FileSystemService _fileSystemService;
        // Service for logging actions, errors, and file transfers
        private readonly LoggerService _loggerService;
        // Service for managing backup states with loading, saving, and updating backup states
        private readonly StateService _stateService;

        // Constructor that initializes the services and loads jobs from a file
        public BackupManagerViewModel(FileSystemService fileSystemService,
                                     LoggerService loggerService,
                                     StateService stateService)
        {
            _fileSystemService = fileSystemService;
            _loggerService = loggerService;
            _stateService = stateService;
            LoadJobsFromFile();
        }

        // Path to the JSON file where jobs are persisted
        private readonly string _jobsFilePath = "jobs.json";

        // Method to save the jobs to the JSON file
        private void SaveJobsToFile()
        {
            try
            {
                // Serialize the list of jobs to JSON and write to the file
                var jobsData = _jobs.Select(vm => vm.Job).ToList();
                // Options for JSON serialization
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Serialize the jobs data to JSON
                var json = JsonSerializer.Serialize(jobsData, options);
                File.WriteAllText(_jobsFilePath, json);
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Erreur lors de la sauvegarde des jobs : {ex.Message}");
            }
        }


        // Method to load jobs from the JSON file
        private void LoadJobsFromFile()
        {
            // Check if the file exists and load the jobs
            if (!File.Exists(_jobsFilePath))
                return;

            // Read the file content and deserialize it into a list of jobs
            var json = File.ReadAllText(_jobsFilePath);
            // Check if the file is empty
            var jobsData = JsonSerializer.Deserialize<List<BackupJob>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // facultatif mais utile
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase // indispensable ici
            });

            // Check if the deserialization was successful
            if (jobsData == null)
                return;

            foreach (var job in jobsData)
            {
                if (job == null)
                    continue;

                // Create a new BackupJobViewModel for each job and add it to the list
                var vm = new BackupJobViewModel(job, _fileSystemService, _loggerService, _stateService);
                _jobs.Add(vm);
                _nextId = Math.Max(_nextId, job.Id + 1);
            }
        }

        // Method to create a new backup job
        public BackupJobViewModel CreateJob(string name, string source, string target, string type)
        {
            // Validate the input parameters
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Job name cannot be null or empty", nameof(name));

            // Check if the source and target paths are valid
            var job = new BackupJob
            {
                Id = _nextId++,
                Name = name,
                SourcePath = Path.GetFullPath(source),
                TargetPath = Path.GetFullPath(target),
                Type = type
            };

            // Check if the source and target directories exist
            var jobViewModel = new BackupJobViewModel(job, _fileSystemService, _loggerService, _stateService);
            _jobs.Add(jobViewModel);
            _stateService.UpdateState(name, "Created", 0);
            _loggerService.Log($"Created new backup job: {name} (ID: {jobViewModel.Id})");
            _loggerService.Log("Sauvegarde des jobs dans le fichier JSON");
            SaveJobsToFile();

            return jobViewModel;
        }

        // Method to delete a backup job
        public void ExecuteJobs(IEnumerable<int> jobIds)
        {
            foreach (var id in jobIds)
            {
                // Find the job with the specified ID
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

        // Method to delete a backup job
        public IEnumerable<BackupJobViewModel> GetAllJobs() => _jobs.AsReadOnly();
    }
}
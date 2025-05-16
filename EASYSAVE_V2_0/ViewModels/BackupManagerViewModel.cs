using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EasySave.Models;
using EasySave.Services;
using System.Text.Json;

namespace EasySave.ViewModels
{
    public class BackupManagerViewModel : ViewModelBase
    {
        private readonly ObservableCollection<BackupJobViewModel> _jobs = new();
        private int _nextId = 1;
        private readonly FileSystemService _fileSystemService;
        private readonly LoggerService _loggerService;
        private readonly StateService _stateService;

        private BackupJobViewModel _selectedJob;
        private bool _isExecutingJobs;

        public BackupManagerViewModel(FileSystemService fileSystemService,
                                     LoggerService loggerService,
                                     StateService stateService)
        {
            _fileSystemService = fileSystemService;
            _loggerService = loggerService;
            _stateService = stateService;

            CreateJobCommand = new RelayCommand(param => CreateJob(param as string[] ?? Array.Empty<string>()));

            ExecuteSelectedJobCommand = new RelayCommand(
                async param => await ExecuteSelectedJobAsync(),
                param => SelectedJob != null && !SelectedJob.IsRunning && !IsExecutingJobs);

            ExecuteAllJobsCommand = new RelayCommand(
                async param => await ExecuteAllJobsAsync(),
                param => _jobs.Count > 0 && !IsExecutingJobs && !_jobs.Any(j => j.IsRunning));

            LoadJobsFromFile();
        }

        public ObservableCollection<BackupJobViewModel> Jobs => _jobs;

        public BackupJobViewModel SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        public bool IsExecutingJobs
        {
            get => _isExecutingJobs;
            private set => SetProperty(ref _isExecutingJobs, value);
        }

        public ICommand CreateJobCommand { get; }
        public ICommand ExecuteSelectedJobCommand { get; }
        public ICommand ExecuteAllJobsCommand { get; }

        public BackupJobViewModel CreateJob(string[] parameters)
        {
            if (parameters.Length < 4)
                throw new ArgumentException("Missing job parameters");

            string name = parameters[0];
            string source = parameters[1];
            string target = parameters[2];
            string type = parameters[3];

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

            SaveJobsToFile();

            return jobViewModel;
        }

        public async Task ExecuteSelectedJobAsync()
        {
            if (SelectedJob == null || SelectedJob.IsRunning)
                return;

            await SelectedJob.ExecuteAsync();
        }

        public async Task ExecuteAllJobsAsync()
        {
            if (IsExecutingJobs || _jobs.Count == 0)
                return;

            IsExecutingJobs = true;

            try
            {
                foreach (var job in _jobs)
                {
                    await job.ExecuteAsync();
                }
            }
            finally
            {
                IsExecutingJobs = false;
            }
        }

        public async Task ExecuteJobsAsync(IEnumerable<int> jobIds)
        {
            if (IsExecutingJobs)
                return;

            IsExecutingJobs = true;

            try
            {
                foreach (var id in jobIds)
                {
                    var job = _jobs.FirstOrDefault(j => j.Id == id);
                    if (job != null)
                    {
                        await job.ExecuteAsync();
                    }
                }
            }
            finally
            {
                IsExecutingJobs = false;
            }
        }

        public bool JobNameExists(string name)
        {
            return _jobs.Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static readonly string JobsFileName = Path.GetFullPath(
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "BackupJobs", "jobs.json"));
        public void SaveJobsToFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(JobsFileName)!);
                var jobModels = _jobs.Select(vm => new BackupJob
                {
                    Id = vm.Id,
                    Name = vm.Name,
                    SourcePath = vm.SourcePath,
                    TargetPath = vm.TargetPath,
                    Type = vm.Type
                }).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(jobModels, options);
                File.WriteAllText(JobsFileName, json);
                _loggerService.Log($"Saved {jobModels.Count} backup jobs to {JobsFileName}");
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Failed to save jobs: {ex.Message}");
            }
        }
        public void LoadJobsFromFile()
        {
            if (!File.Exists(JobsFileName))
                return;
            try
            {
                string json = File.ReadAllText(JobsFileName);
                var jobModels = JsonSerializer.Deserialize<List<BackupJob>>(json);
                if (jobModels == null)
                    return;
                _jobs.Clear();
                foreach (var jobModel in jobModels)
                {
                    if (jobModel.Id >= _nextId)
                        _nextId = jobModel.Id + 1;
                    var jobVM = new BackupJobViewModel(jobModel, _fileSystemService, _loggerService, _stateService);
                    _jobs.Add(jobVM);
                }
                _loggerService.Log($"Loaded {jobModels.Count} backup jobs from {JobsFileName}");
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Failed to load jobs: {ex.Message}");
            }
        }
    }
}
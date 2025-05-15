using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using EasySave.Models;
using EasySave.Services;

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

        private bool _showDeleteConfirmation;
        private BackupJobViewModel _jobToDelete;

        public bool ShowDeleteConfirmation
        {
            get => _showDeleteConfirmation;
            set => SetProperty(ref _showDeleteConfirmation, value);
        }

        public BackupJobViewModel JobToDelete
        {
            get => _jobToDelete;
            set => SetProperty(ref _jobToDelete, value);
        }

        public ICommand DeleteJobCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }


        public BackupManagerViewModel(FileSystemService fileSystemService,
                                     LoggerService loggerService,
                                     StateService stateService)
        {
            _fileSystemService = fileSystemService;
            _loggerService = loggerService;
            _stateService = stateService;

            CreateJobCommand = new RelayCommand(param => CreateJob(param as string[] ?? Array.Empty<string>()),
                param => CanCreateJob());

            ExecuteSelectedJobCommand = new RelayCommand(
                async param => await ExecuteSelectedJobAsync(),
                param => SelectedJob != null && !SelectedJob.IsRunning && !IsExecutingJobs);

            ExecuteAllJobsCommand = new RelayCommand(
                async param => await ExecuteAllJobsAsync(),
                param => _jobs.Count > 0 && !IsExecutingJobs && !_jobs.Any(j => j.IsRunning));

            DeleteJobCommand = new RelayCommand(
                param => RequestDeleteJob(param as BackupJobViewModel),
                param => param is BackupJobViewModel job && !job.IsRunning);

            ConfirmDeleteCommand = new RelayCommand(
                param => DeleteJob(),
                param => JobToDelete != null);

            CancelDeleteCommand = new RelayCommand(
                param => CancelDelete());
        }


        private void RequestDeleteJob(BackupJobViewModel job)
        {
            if (job == null) return;

            JobToDelete = job;
            ShowDeleteConfirmation = true;
        }

        private void DeleteJob()
        {
            if (JobToDelete == null) return;

            _loggerService.Log($"Deleted backup job: {JobToDelete.Name} (ID: {JobToDelete.Id})");
            _jobs.Remove(JobToDelete);

            ShowDeleteConfirmation = false;
            JobToDelete = null;
        }

        private void CancelDelete()
        {
            ShowDeleteConfirmation = false;
            JobToDelete = null;
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

        public bool CanCreateJob()
        {
            return _jobs.Count < 5;
        }

        public bool JobNameExists(string name)
        {
            return _jobs.Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }


    }
}
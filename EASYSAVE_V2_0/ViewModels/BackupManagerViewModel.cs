using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using EasySave.Models;
using EasySave.Services;

namespace EasySave.ViewModels
{
    public class BackupManagerViewModel : ViewModelBase
    {
        private readonly ObservableCollection<BackupJobViewModel> _jobs = new();
        private readonly FileSystemService _fileSystemService;
        private readonly LoggerService _loggerService;
        private readonly StateService _stateService;
        private readonly PersistenceService _persistenceService;

        private int _nextId = 1;
        private BackupJobViewModel _selectedJob;
        private bool _showEditDialog;
        private BackupJobViewModel _jobToEdit;

        public BackupManagerViewModel(
            FileSystemService fileSystemService,
            LoggerService loggerService,
            StateService stateService,
            PersistenceService persistenceService)
        {
            _fileSystemService = fileSystemService;
            _loggerService = loggerService;
            _stateService = stateService;
            _persistenceService = persistenceService;

            LoadInitialJobs();

            CreateJobCommand = new RelayCommand(CreateJob, CanCreateJob);
            ExecuteSelectedJobCommand = new RelayCommand(async _ => await ExecuteSelectedJobAsync(), _ => SelectedJob != null && !SelectedJob.IsRunning);
            ExecuteAllJobsCommand = new RelayCommand(async _ => await ExecuteAllJobsAsync(), _ => _jobs.Count > 0 && !_jobs.Any(j => j.IsRunning));
            DeleteSelectedJobsCommand = new RelayCommand(DeleteSelectedJobsWithConfirmation, _ => _jobs.Any(j => j.IsSelected));
            ConfirmEditCommand = new RelayCommand(_ => ConfirmEdit());
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
        }

        public ObservableCollection<BackupJobViewModel> Jobs => _jobs;
        public BackupJobViewModel SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        public bool ShowEditDialog
        {
            get => _showEditDialog;
            set => SetProperty(ref _showEditDialog, value);
        }

        public BackupJobViewModel JobToEdit
        {
            get => _jobToEdit;
            set => SetProperty(ref _jobToEdit, value);
        }

        public ICommand CreateJobCommand { get; }
        public ICommand ExecuteSelectedJobCommand { get; }
        public ICommand ExecuteAllJobsCommand { get; }
        public ICommand DeleteSelectedJobsCommand { get; }
        public ICommand ConfirmEditCommand { get; }
        public ICommand CancelEditCommand { get; }

        private void LoadInitialJobs()
        {
            var savedJobs = _persistenceService.LoadJobs();
            foreach (var job in savedJobs)
            {
                _jobs.Add(new BackupJobViewModel(job, _fileSystemService, _loggerService, _stateService));
                _nextId = Math.Max(_nextId, job.Id + 1);
            }
        }

        public void SaveJobs()
        {
            _persistenceService.SaveJobs(_jobs.Select(j => j.GetBackupJob()));
        }

        public bool JobNameExists(string name)
        {
            return _jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void CreateJob(object parameters)
        {
            if (parameters is not string[] jobParams || jobParams.Length < 4)
                throw new ArgumentException("Invalid job parameters");

            string name = jobParams[0];
            string source = jobParams[1];
            string target = jobParams[2];
            string type = jobParams[3];

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Job name cannot be empty");

            if (JobNameExists(name))
                throw new ArgumentException("Job name already exists");

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
            SaveJobs();
        }

        public bool CanCreateJob(object _)
        {
            return _jobs.Count < 5;
        }

        private async Task ExecuteSelectedJobAsync()
        {
            if (SelectedJob == null || SelectedJob.IsRunning) return;
            await SelectedJob.ExecuteAsync();
        }

        private async Task ExecuteAllJobsAsync()
        {
            var tasks = _jobs.Where(j => !j.IsRunning)
                           .Select(job => Task.Run(() => job.ExecuteAsync()))
                           .ToList();
            await Task.WhenAll(tasks);
        }

        private void DeleteSelectedJobsWithConfirmation(object _)
        {
            var jobsToDelete = _jobs.Where(j => j.IsSelected).ToList();
            if (!jobsToDelete.Any()) return;

            var message = $"Are you sure you want to delete {jobsToDelete.Count} selected job(s)?";
            var result = MessageBox.Show(message, "Confirm Deletion",
                                      MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var job in jobsToDelete)
                {
                    _loggerService.Log($"Deleted backup job: {job.Name} (ID: {job.Id})");
                    _stateService.UpdateState(job.Name, "Deleted", 0);
                    _jobs.Remove(job);
                }
                SaveJobs();
            }
        }

        public void RequestEditJob(BackupJobViewModel job)
        {
            JobToEdit = new BackupJobViewModel(
                new BackupJob
                {
                    Id = job.Id,
                    Name = job.Name,
                    SourcePath = job.SourcePath,
                    TargetPath = job.TargetPath,
                    Type = job.Type,
                    IsSelected = job.IsSelected
                },
                _fileSystemService,
                _loggerService,
                _stateService);
            ShowEditDialog = true;
        }

        private void ConfirmEdit()
        {
            if (JobToEdit == null) return;

            var originalJob = _jobs.FirstOrDefault(j => j.Id == JobToEdit.Id);
            if (originalJob != null)
            {
                originalJob.Name = JobToEdit.Name;
                originalJob.SourcePath = JobToEdit.SourcePath;
                originalJob.TargetPath = JobToEdit.TargetPath;
                originalJob.Type = JobToEdit.Type;

                _loggerService.Log($"Edited backup job: {originalJob.Name} (ID: {originalJob.Id})");
                _stateService.UpdateState(originalJob.Name, "Modified", originalJob.Progress);
                SaveJobs();
            }
            ShowEditDialog = false;
        }

        private void CancelEdit()
        {
            ShowEditDialog = false;
        }
    }
}
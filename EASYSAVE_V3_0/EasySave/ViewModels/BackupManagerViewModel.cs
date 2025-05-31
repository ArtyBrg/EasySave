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
using System.Windows.Forms;
using System.Text.Json;
using EasySave_WPF;

namespace EasySave.ViewModels
{
    // Manages the backup jobs and their execution
    public class BackupManagerViewModel : ViewModelBase
    {
        // Collection of backup job view models
        private readonly ObservableCollection<BackupJobViewModel> _jobs = new();
        // Service for file system operations
        private readonly FileSystemService _fileSystemService;
        // Service for logging
        private readonly LoggerService _loggerService;
        // Service for managing backup job states
        private readonly StateService _stateService;
        // Service for persisting jobs to disk
        private readonly PersistenceService _persistenceService;

        // Unique ID for each job, incremented for each new job
        private int _nextId = 1;
        // Currently selected job in the UI
        private BackupJobViewModel _selectedJob;
        // Indicates if the edit dialog is shown
        private bool _showEditDialog;
        // The job currently being edited
        private BackupJobViewModel _jobToEdit;
        // Token source for cancelling running jobs
        private System.Threading.CancellationTokenSource _cts = new System.Threading.CancellationTokenSource();
        // Indicates if jobs are currently being executed
        private bool _isExecutingJobs = false;

        // Constructor for the BackupManagerViewModel
        public BackupManagerViewModel(
            FileSystemService fileSystemService,
            LoggerService loggerService,
            StateService stateService,
            PersistenceService persistenceService)
        {
            // Initialize services
            _fileSystemService = fileSystemService;
            _loggerService = loggerService;
            _stateService = stateService;
            _persistenceService = persistenceService;

            // Load jobs from persistence and file
            LoadInitialJobs();
            LoadJobsFromFile();

            // Initialize commands for UI actions
            CreateJobCommand = new RelayCommand(param => CreateJob(param as string[] ?? Array.Empty<string>()), CanCreateJob);
            ExecuteSelectedJobCommand = new RelayCommand(async _ => await ExecuteSelectedJobAsync(), _ => _jobs.Any(j => j.IsSelected && !j.IsRunning && !_isExecutingJobs));
            ExecuteAllJobsCommand = new RelayCommand(async _ => await ExecuteAllJobsAsync(), _ => _jobs.Count > 0 && !_isExecutingJobs && !_jobs.Any(j => j.IsRunning));
            ConfirmEditCommand = new RelayCommand(_ => ConfirmEdit());
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            DeleteSelectedJobsCommand = new RelayCommand(DeleteSelectedJobsWithConfirmation, _ => Jobs.Any(j => j.IsSelected));
            RequestEditJobCommand = new RelayCommand(parameter => RequestEditJob(parameter as BackupJobViewModel));
            ExecuteJobsSequentiallyCommand = new RelayCommand(async _ => await ExecuteJobsSequentially(), _ => _jobs.Any(j => j.IsSelected && !j.IsRunning && !_isExecutingJobs));
            CancelAllJobsCommand = new RelayCommand(_ => CancelAllRunningJobs(), _ => _jobs.Any(j => j.IsRunning));
            BrowseSourceCommand = new RelayCommand(parameter => BrowsePath(parameter as BackupJobViewModel, true));
            BrowseTargetCommand = new RelayCommand(parameter => BrowsePath(parameter as BackupJobViewModel, false));
        }

        // Exposes the jobs collection for data binding
        public ObservableCollection<BackupJobViewModel> Jobs => _jobs;
        // Property for the selected job
        public BackupJobViewModel SelectedJob
        {
            get => _selectedJob;
            set => SetProperty(ref _selectedJob, value);
        }

        // Property for showing the edit dialog
        public bool ShowEditDialog
        {
            get => _showEditDialog;
            set => SetProperty(ref _showEditDialog, value);
        }

        // Property for the job to edit
        public BackupJobViewModel JobToEdit
        {
            get => _jobToEdit;
            set => SetProperty(ref _jobToEdit, value);
        }

        // Commands for UI actions
        public ICommand CreateJobCommand { get; }
        public ICommand ExecuteSelectedJobCommand { get; }
        public ICommand ExecuteAllJobsCommand { get; }
        public ICommand ConfirmEditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteSelectedJobsCommand { get; }
        public ICommand RequestEditJobCommand { get; }
        public ICommand ExecuteJobsSequentiallyCommand { get; }
        public ICommand CancelAllJobsCommand { get; }
        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseTargetCommand { get; }

        // Indicates if jobs are currently being executed and updates command states
        private bool IsExecutingJobs
        {
            get => _isExecutingJobs;
            set
            {
                _isExecutingJobs = value;
                ((RelayCommand)ExecuteSelectedJobCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExecuteAllJobsCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExecuteJobsSequentiallyCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DeleteSelectedJobsCommand).RaiseCanExecuteChanged();
            }
        }

        // Loads jobs from the persistence service
        private void LoadInitialJobs()
        {
            var savedJobs = _persistenceService.LoadJobs();
            foreach (var job in savedJobs)
            {
                _jobs.Add(new BackupJobViewModel(job, _fileSystemService, _loggerService, _stateService));
                _nextId = Math.Max(_nextId, job.Id + 1);
            }
        }

        // Saves jobs to the persistence service
        public void SaveJobs()
        {
            _persistenceService.SaveJobs(_jobs.Select(j => j.GetBackupJob()));
        }

        // Checks if a job with the given name already exists
        public bool JobNameExists(string name)
        {
            return _jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        // Creates a new job with the given parameters
        public BackupJobViewModel CreateJob(string[] parameters)
        {
            if (parameters.Length < 4)
                throw new ArgumentException("Missing job parameters");

            string name = parameters[0];
            string source = parameters[1];
            string target = parameters[2];
            string type = parameters[3];

            // Validate the job name
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

        // Checks if a new job can be created (limit to 5)
        public bool CanCreateJob(object _)
        {
            return _jobs.Count < 5;
        }

        // Executes the selected jobs asynchronously
        private async Task ExecuteSelectedJobAsync()
        {
            var selectedJobs = _jobs.Where(j => j.IsSelected && !j.IsRunning).ToList();
            if (!selectedJobs.Any()) return;

            IsExecutingJobs = true;

            var tasks = selectedJobs.Select(async job =>
            {
                try
                {
                    await job.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    _loggerService.LogError($"Error executing job {job.Name}: {ex.Message}");
                }
            }).ToList();

            await Task.WhenAll(tasks);

            IsExecutingJobs = false;
        }

        // Executes all jobs asynchronously
        private async Task ExecuteAllJobsAsync()
        {
            var jobsToExecute = _jobs.Where(j => !j.IsRunning).ToList();
            if (!jobsToExecute.Any()) return;

            IsExecutingJobs = true;
            var tasks = jobsToExecute.Select(job => Task.Run(() => job.ExecuteAsync())).ToList();
            await Task.WhenAll(tasks);
            IsExecutingJobs = false;
        }

        // Executes jobs with the given IDs asynchronously
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

        // Deletes selected jobs after user confirmation
        private void DeleteSelectedJobsWithConfirmation(object parameter)
        {
            var selectedJobs = Jobs.Where(j => j.IsSelected).ToList();
            if (!selectedJobs.Any()) return;

            var message = $"Voulez-vous vraiment supprimer {selectedJobs.Count} job(s) sélectionné(s) ?";
            var result = System.Windows.MessageBox.Show(message, "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var job in selectedJobs.ToList())
                {
                    _loggerService.Log($"Job supprimé : {job.Name} (ID: {job.Id})");
                    _stateService.UpdateState(job.Name, "Deleted", 0);
                    Jobs.Remove(job);
                }
                SaveJobsToFile();
            }
        }

        // Requests to edit a job and opens the edit dialog
        public void RequestEditJob(BackupJobViewModel job)
        {
            if (job == null) return;

            // Create a new instance of BackupJobViewModel for editing
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

        // Confirms the edit of a job and updates the job in the collection
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
                SaveJobsToFile();
            }
            ShowEditDialog = false;
        }

        // Cancels the edit of a job and closes the edit dialog
        private void CancelEdit()
        {
            ShowEditDialog = false;
        }

        // Executes selected jobs sequentially (one after another)
        public async Task ExecuteJobsSequentially()
        {
            var selectedJobs = Jobs.Where(j => j.IsSelected && !j.IsRunning).ToList();
            if (!selectedJobs.Any()) return;

            _cts = new System.Threading.CancellationTokenSource();
            IsExecutingJobs = true;

            foreach (var job in selectedJobs)
            {
                if (_cts.Token.IsCancellationRequested)
                    break;

                await job.ExecuteAsync(_cts.Token);
            }
            IsExecutingJobs = false;
        }

        // Cancels all running jobs
        private void CancelAllRunningJobs()
        {
            _cts?.Cancel();
            foreach (var job in Jobs.Where(j => j.IsRunning))
            {
                job.StopJob();
            }
            _cts = new System.Threading.CancellationTokenSource(); // Reset the cancellation token
        }

        // Opens a folder browser dialog to select a source or target path
        private void BrowsePath(BackupJobViewModel job, bool isSource)
        {
            if (job == null) return;

            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (isSource)
                    job.SourcePath = dialog.SelectedPath;
                else
                    job.TargetPath = dialog.SelectedPath;
            }
        }

        // Path to the jobs file on disk
        private static readonly string JobsFileName = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "BackupJobs", "jobs.json"));

        // Saves the jobs to a JSON file
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

        // Loads jobs from a JSON file
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

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
        private System.Threading.CancellationTokenSource _cts = new System.Threading.CancellationTokenSource();
        private bool _isExecutingJobs = false;

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
            LoadJobsFromFile();

            CreateJobCommand = new RelayCommand(param => CreateJob(param as string[] ?? Array.Empty<string>()), CanCreateJob);
            ExecuteSelectedJobCommand = new RelayCommand(async _ => await ExecuteSelectedJobAsync(), _ => _jobs.Any(j => j.IsSelected && !j.IsRunning && !_isExecutingJobs));
            ExecuteAllJobsCommand = new RelayCommand(async _ => await ExecuteAllJobsAsync(), _ => _jobs.Count > 0 && !_isExecutingJobs && !_jobs.Any(j => j.IsRunning));
            ConfirmEditCommand = new RelayCommand(_ => ConfirmEdit());
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            DeleteSelectedJobsCommand = new RelayCommand(DeleteSelectedJobsWithConfirmation);
            RequestEditJobCommand = new RelayCommand(parameter => RequestEditJob(parameter as BackupJobViewModel));
            ExecuteJobsSequentiallyCommand = new RelayCommand(async _ => await ExecuteJobsSequentially(), _ => _jobs.Any(j => j.IsSelected && !j.IsRunning && !_isExecutingJobs));
            CancelAllJobsCommand = new RelayCommand(_ => CancelAllRunningJobs(), _ => _jobs.Any(j => j.IsRunning));
            BrowseSourceCommand = new RelayCommand(parameter => BrowsePath(parameter as BackupJobViewModel, true));
            BrowseTargetCommand = new RelayCommand(parameter => BrowsePath(parameter as BackupJobViewModel, false));
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
        public ICommand ConfirmEditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand DeleteSelectedJobsCommand { get; }
        public ICommand RequestEditJobCommand { get; }
        public ICommand ExecuteJobsSequentiallyCommand { get; }
        public ICommand CancelAllJobsCommand { get; }
        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseTargetCommand { get; }

        private bool IsExecutingJobs
        {
            get => _isExecutingJobs;
            set
            {
                _isExecutingJobs = value;
                ((RelayCommand)ExecuteSelectedJobCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExecuteAllJobsCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExecuteJobsSequentiallyCommand).RaiseCanExecuteChanged();
            }
        }

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

        public bool CanCreateJob(object _)
        {
            return _jobs.Count < 5;
        }

        private async Task ExecuteSelectedJobAsync()
        {
            var selectedJobs = _jobs.Where(j => j.IsSelected && !j.IsRunning).ToList();
            if (!selectedJobs.Any()) return;

            IsExecutingJobs = true;
            foreach (var job in selectedJobs)
            {
                try
                {
                    await job.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    _loggerService.LogError($"Error executing job {job.Name}: {ex.Message}");
                }
            }
            IsExecutingJobs = false;
        }

        private async Task ExecuteAllJobsAsync()
        {
            var jobsToExecute = _jobs.Where(j => !j.IsRunning).ToList();
            if (!jobsToExecute.Any()) return;

            IsExecutingJobs = true;
            var tasks = jobsToExecute.Select(job => Task.Run(() => job.ExecuteAsync())).ToList();
            await Task.WhenAll(tasks);
            IsExecutingJobs = false;
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

        public void RequestEditJob(BackupJobViewModel job)
        {
            if (job == null) return;

            // Crée une copie du job à éditer
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
                SaveJobsToFile();
            }
            ShowEditDialog = false;
        }

        private void CancelEdit()
        {
            ShowEditDialog = false;
        }

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

        private void CancelAllRunningJobs()
        {
            _cts?.Cancel();
            foreach (var job in Jobs.Where(j => j.IsRunning))
            {
                job.StopJob();
            }
            _cts = new System.Threading.CancellationTokenSource(); // Reset the cancellation token
        }

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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
            DeleteSelectedJobsCommand = new RelayCommand(_ => DeleteSelectedJobs(), _ => _jobs.Any(j => j.IsSelected));
            EditSelectedJobCommand = new RelayCommand(_ => RequestEditJob(), _ => SelectedJob != null && !SelectedJob.IsRunning);
            ConfirmEditCommand = new RelayCommand(_ => ConfirmEdit());
            CancelEditCommand = new RelayCommand(_ => CancelEdit());
            BrowseSourceCommand = new RelayCommand(_ => BrowseSource());
            BrowseTargetCommand = new RelayCommand(_ => BrowseTarget());
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

        public int SelectedJobsCount => _jobs.Count(j => j.IsSelected);

        public ICommand CreateJobCommand { get; }
        public ICommand ExecuteSelectedJobCommand { get; }
        public ICommand ExecuteAllJobsCommand { get; }
        public ICommand DeleteSelectedJobsCommand { get; }
        public ICommand EditSelectedJobCommand { get; }
        public ICommand ConfirmEditCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseTargetCommand { get; }

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

        public bool CanCreateJob(object _)
        {
            return _jobs.Count < 5;
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

        private async Task ExecuteSelectedJobAsync()
        {
            if (SelectedJob == null || SelectedJob.IsRunning) return;
            await SelectedJob.ExecuteAsync();
        }

        private async Task ExecuteAllJobsAsync()
        {
            foreach (var job in _jobs.Where(j => !j.IsRunning))
            {
                await job.ExecuteAsync();
            }
        }

        private void DeleteSelectedJobs()
        {
            var jobsToDelete = _jobs.Where(j => j.IsSelected).ToList();
            foreach (var job in jobsToDelete)
            {
                _loggerService.Log($"Deleted backup job: {job.Name} (ID: {job.Id})");
                _stateService.UpdateState(job.Name, "Deleted", 0);
                _jobs.Remove(job);
            }
            SaveJobs();
        }

        private void RequestEditJob()
        {
            if (SelectedJob == null) return;
            JobToEdit = SelectedJob;
            ShowEditDialog = true;
        }

        private void ConfirmEdit()
        {
            if (JobToEdit == null) return;
            _loggerService.Log($"Edited backup job: {JobToEdit.Name} (ID: {JobToEdit.Id})");
            _stateService.UpdateState(JobToEdit.Name, "Modified", JobToEdit.Progress);
            ShowEditDialog = false;
            SaveJobs();
        }

        private void CancelEdit()
        {
            ShowEditDialog = false;
            JobToEdit = null;
        }

        private void BrowseSource()
        {
            if (JobToEdit == null) return;

            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                JobToEdit.SourcePath = dialog.SelectedPath;
            }
        }

        private void BrowseTarget()
        {
            if (JobToEdit == null) return;

            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                JobToEdit.TargetPath = dialog.SelectedPath;
            }
        }
    }
}
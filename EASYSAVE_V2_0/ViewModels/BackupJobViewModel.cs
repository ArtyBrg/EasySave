using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EasySave.Models;
using EasySave.Services;

namespace EasySave.ViewModels
{
    public class BackupJobViewModel : ViewModelBase
    {
        private readonly BackupJob _backupJob;
        private readonly FileSystemService _fileSystemService;
        private readonly LoggerService _loggerService;
        private readonly StateService _stateService;

        private bool _isRunning;
        private double _progress;
        private bool _isPaused;
        private bool _isSelected;

        public BackupJobViewModel(BackupJob backupJob,
                        FileSystemService fileSystemService,
                        LoggerService loggerService,
                        StateService stateService)
        {
            _backupJob = backupJob ?? throw new ArgumentNullException(nameof(backupJob));
            _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));

            ExecuteCommand = new RelayCommand(async _ => await ExecuteAsync(), _ => !IsRunning);
            PauseCommand = new RelayCommand(_ => PauseJob(), _ => IsRunning && !IsPaused);
            StopCommand = new RelayCommand(_ => StopJob(), _ => IsRunning);
        }

        public int Id => _backupJob.Id;

        public string Name
        {
            get => _backupJob.Name;
            set
            {
                if (_backupJob.Name != value)
                {
                    _backupJob.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SourcePath
        {
            get => _backupJob.SourcePath;
            set
            {
                if (_backupJob.SourcePath != value)
                {
                    _backupJob.SourcePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TargetPath
        {
            get => _backupJob.TargetPath;
            set
            {
                if (_backupJob.TargetPath != value)
                {
                    _backupJob.TargetPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Type
        {
            get => _backupJob.Type;
            set
            {
                if (_backupJob.Type != value)
                {
                    _backupJob.Type = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _backupJob.IsSelected;
            set
            {
                if (_backupJob.IsSelected != value)
                {
                    _backupJob.IsSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set => SetProperty(ref _isRunning, value);
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public bool IsPaused
        {
            get => _isPaused;
            private set => SetProperty(ref _isPaused, value);
        }

        public RelayCommand ExecuteCommand { get; }
        public RelayCommand PauseCommand { get; }
        public RelayCommand StopCommand { get; }

        public BackupJob GetBackupJob() => _backupJob;

        private void PauseJob()
        {
            IsPaused = !IsPaused;
            _loggerService.Log($"Backup job {Name} {(IsPaused ? "paused" : "resumed")}");
            ((RelayCommand)PauseCommand).RaiseCanExecuteChanged();
        }

        private void StopJob()
        {
            IsRunning = false;
            IsPaused = false;
            _loggerService.Log($"Backup job {Name} stopped");
            _stateService.UpdateState(Name, "Stopped", Progress);
            ((RelayCommand)ExecuteCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PauseCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopCommand).RaiseCanExecuteChanged();
        }

        public async Task ExecuteAsync()
        {
            if (IsRunning) return;

            IsRunning = true;
            IsPaused = false;
            Progress = 0;

            try
            {
                _loggerService.Log($"Starting backup job {Id} - {Name}");
                _stateService.UpdateState(Name, "Active", 0, SourcePath, TargetPath);

                await Task.Run(() =>
                {
                    if (!Directory.Exists(SourcePath))
                        throw new DirectoryNotFoundException($"Source directory not found: {SourcePath}");

                    Directory.CreateDirectory(TargetPath);

                    if (Type == "Complete")
                        ExecuteCompleteBackup();
                    else
                        ExecuteDifferentialBackup();
                });

                _stateService.UpdateState(Name, "Completed", 100);
                _loggerService.Log($"Backup job {Name} completed successfully");
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Error in backup job {Name}: {ex.Message}");
                _stateService.UpdateState(Name, "Failed", Progress);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void ExecuteCompleteBackup()
        {
            var allFiles = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories).ToList();
            long totalSize = allFiles.Sum(f => new FileInfo(f).Length);
            int totalFiles = allFiles.Count;

            _stateService.UpdateState(Name, "InProgress", 0, filesRemaining: totalFiles, totalFiles: totalFiles, totalSize: totalSize);

            for (int i = 0; i < allFiles.Count; i++)
            {
                if (!IsRunning) break;
                while (IsPaused) Thread.Sleep(500);

                string sourceFile = allFiles[i];
                string relativePath = sourceFile.Substring(SourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                CopyFileWithProgress(sourceFile, targetFile, i, totalFiles, totalSize);
            }
        }

        private void ExecuteDifferentialBackup()
        {
            DateTime lastBackupDate = GetLastBackupDate();
            var modifiedFiles = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories)
                                      .Where(f => File.GetLastWriteTime(f) > lastBackupDate)
                                      .ToList();

            long totalSize = modifiedFiles.Sum(f => new FileInfo(f).Length);
            int totalFiles = modifiedFiles.Count;

            _stateService.UpdateState(Name, "InProgress", 0, filesRemaining: totalFiles, totalFiles: totalFiles, totalSize: totalSize);

            for (int i = 0; i < modifiedFiles.Count; i++)
            {
                if (!IsRunning) break;
                while (IsPaused) Thread.Sleep(500);

                string sourceFile = modifiedFiles[i];
                string relativePath = sourceFile.Substring(SourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                CopyFileWithProgress(sourceFile, targetFile, i, totalFiles, totalSize);
            }
        }

        private DateTime GetLastBackupDate()
        {
            return Directory.Exists(TargetPath)
                ? Directory.GetLastWriteTime(TargetPath)
                : DateTime.MinValue;
        }

        private void CopyFileWithProgress(string sourceFile, string targetFile, int currentIndex, int totalFiles, long totalSize)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                var fileInfo = new FileInfo(sourceFile);
                File.Copy(sourceFile, targetFile, true);

                double progress = (currentIndex + 1) * 100.0 / totalFiles;
                Progress = progress;

                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    progress,
                    filesRemaining: totalFiles - (currentIndex + 1)
                );
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Failed to copy {sourceFile}: {ex.Message}");
            }
        }
    }
}
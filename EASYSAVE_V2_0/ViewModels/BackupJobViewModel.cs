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
        private CancellationTokenSource _cts;

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
        }

        private void StopJob()
        {
            _cts?.Cancel();
            IsRunning = false;
            IsPaused = false;
            _loggerService.Log($"Backup job {Name} stopped");
            _stateService.UpdateState(Name, "Stopped", Progress);
        }

        public async Task ExecuteAsync()
        {
            if (IsRunning) return;

            IsRunning = true;
            IsPaused = false;
            Progress = 0;
            _cts = new CancellationTokenSource();

            try
            {
                _loggerService.Log($"Starting backup job {Id} - {Name}");
                _stateService.UpdateState(Name, "Active", 0, SourcePath, TargetPath);

                await Task.Run(async () =>
                {
                    if (!Directory.Exists(SourcePath))
                        throw new DirectoryNotFoundException($"Source directory not found: {SourcePath}");

                    Directory.CreateDirectory(TargetPath);

                    if (Type == "Complete")
                        await ExecuteCompleteBackupAsync();
                    else
                        await ExecuteDifferentialBackupAsync();
                }, _cts.Token);

                _stateService.UpdateState(Name, "Completed", 100);
                _loggerService.Log($"Backup job {Name} completed successfully");
            }
            catch (OperationCanceledException)
            {
                _loggerService.Log($"Backup job {Name} was cancelled");
                _stateService.UpdateState(Name, "Cancelled", Progress);
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Error in backup job {Name}: {ex.Message}");
                _stateService.UpdateState(Name, "Failed", Progress);
            }
            finally
            {
                IsRunning = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task ExecuteCompleteBackupAsync()
        {
            var allFiles = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories).ToList();
            long totalSize = allFiles.Sum(f => new FileInfo(f).Length);
            int totalFiles = allFiles.Count;

            _stateService.UpdateState(Name, "InProgress", 0, filesRemaining: totalFiles, totalFiles: totalFiles, totalSize: totalSize);

            for (int i = 0; i < allFiles.Count; i++)
            {
                _cts.Token.ThrowIfCancellationRequested();
                
                while (IsPaused && !_cts.IsCancellationRequested)
                {
                    await Task.Delay(500, _cts.Token);
                }

                string sourceFile = allFiles[i];
                string relativePath = sourceFile.Substring(SourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                await CopyFileWithProgressAsync(sourceFile, targetFile, i, totalFiles, totalSize);
            }
        }

        private async Task ExecuteDifferentialBackupAsync()
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
                _cts.Token.ThrowIfCancellationRequested();
                
                while (IsPaused && !_cts.IsCancellationRequested)
                {
                    await Task.Delay(500, _cts.Token);
                }

                string sourceFile = modifiedFiles[i];
                string relativePath = sourceFile.Substring(SourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                await CopyFileWithProgressAsync(sourceFile, targetFile, i, totalFiles, totalSize);
            }
        }

        private DateTime GetLastBackupDate()
        {
            return Directory.Exists(TargetPath)
                ? Directory.GetLastWriteTime(TargetPath)
                : DateTime.MinValue;
        }

        private async Task CopyFileWithProgressAsync(string sourceFile, string targetFile, int currentIndex, int totalFiles, long totalSize)
        {
            try
            {
                var targetDir = Path.GetDirectoryName(targetFile);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var fileInfo = new FileInfo(sourceFile);
                using (var sourceStream = File.OpenRead(sourceFile))
                using (var targetStream = File.Create(targetFile))
                {
                    var buffer = new byte[81920];
                    int bytesRead;
                    long totalBytesRead = 0;

                    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                    {
                        await targetStream.WriteAsync(buffer, 0, bytesRead, _cts.Token);
                        totalBytesRead += bytesRead;

                        double progress = (currentIndex + (totalBytesRead / (double)fileInfo.Length)) * 100.0 / totalFiles;
                        Progress = progress;
                    }
                }

                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    (currentIndex + 1) * 100.0 / totalFiles,
                    filesRemaining: totalFiles - (currentIndex + 1)
                );

                _loggerService.LogFileTransfer(Name, sourceFile, targetFile, fileInfo.Length, 0);
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Failed to copy {sourceFile}: {ex.Message}");
                throw;
            }
        }
    }
}
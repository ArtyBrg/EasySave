using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
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
        private bool _stopRequested; // Nouveau drapeau pour demander l'arrêt
        private CancellationTokenSource _cts = new CancellationTokenSource();

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
            PauseCommand = new RelayCommand(_ => PauseJob(), _ => IsRunning);
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

        public bool StopRequested
        {
            get => _stopRequested;
            private set => SetProperty(ref _stopRequested, value);
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

        public void StopJob()
        {
            try
            {
                StopRequested = true; // Demande l'arrêt via le drapeau
                _loggerService.Log($"Job {Name} stop requested");
                // Ne pas appeler _cts.Cancel() qui provoque l'exception si la tâche n'a pas démarré.
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Error requesting stop for job {Name}: {ex.Message}");
            }
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (IsRunning)
            {
                _loggerService.Log($"Job {Name} is already running");
                return;
            }

            IsRunning = true;
            IsPaused = false;
            Progress = 0;
            StopRequested = false; // Réinitialiser le drapeau d'arrêt

            try
            {
                _loggerService.Log($"Starting backup job {Id} - {Name}");
                _stateService.UpdateState(Name, "Active", 0, SourcePath, TargetPath);

                // Utiliser ConfigureAwait(false) pour éviter les problèmes de contexte de synchronisation
                await Task.Run(() =>
                {
                    try
                    {
                        if (Type == "Complete")
                            ExecuteCompleteBackup(cancellationToken);
                        else
                            ExecuteDifferentialBackup(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _loggerService.LogError($"Erreur pendant l'exécution du job {Name}: {ex.Message}");
                        throw;
                    }
                }, cancellationToken).ConfigureAwait(false);

                if (!StopRequested)
                {
                    _stateService.UpdateState(Name, "Completed", 100);
                    _loggerService.Log($"Backup job {Name} completed successfully");
                }
                else
                {
                    _stateService.UpdateState(Name, "Stopped", Progress);
                    _loggerService.Log($"Backup job {Name} stopped by user");
                }
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Erreur dans job {Name}: {ex.Message}");
                _stateService.UpdateState(Name, "Failed", Progress);
            }
            finally
            {
                IsRunning = false;
                StopRequested = false;
            }
        }

        private void ExecuteCompleteBackup(CancellationToken cancellationToken)
        {
            var allFiles = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories).ToList();
            long totalSize = allFiles.Sum(f => new FileInfo(f).Length);
            int totalFiles = allFiles.Count;

            _stateService.UpdateState(Name, "InProgress", 0, filesRemaining: totalFiles, totalFiles: totalFiles, totalSize: totalSize);

            for (int i = 0; i < allFiles.Count && !StopRequested; i++) // Vérifier StopRequested à chaque itération
            {
                // Ne pas utiliser ThrowIfCancellationRequested qui lance l'exception
                if (StopRequested)
                {
                    _loggerService.Log($"Job {Name} arrêté par l'utilisateur avant fichier {i + 1}/{totalFiles}");
                    return; // Sortir proprement sans lancer d'exception
                }

                while (IsPaused && !StopRequested)
                {
                    Thread.Sleep(500);
                }

                if (StopRequested) return; // Double vérification après la pause

                string sourceFile = allFiles[i];
                string relativePath = sourceFile.Substring(SourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                CopyFileWithProgress(sourceFile, targetFile, i, totalFiles, totalSize);

                if (i < allFiles.Count - 1 && !StopRequested)
                {
                    Thread.Sleep(1000); // Délai de 1000 millisecondes (1 seconde)
                }
            }
        }

        // Version modifiée qui n'utilise plus CancellationToken pour l'arrêt direct
        private void CopyFileWithProgress(string sourceFile, string targetFile, int currentIndex, int totalFiles, long totalSize)
        {
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            var fileInfo = new FileInfo(sourceFile);

            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[81920];
                int bytesRead;
                long totalBytesRead = 0;

                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0 && !StopRequested)
                {
                    targetStream.Write(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    double progress = (currentIndex + (totalBytesRead / (double)fileInfo.Length)) * 100.0 / totalFiles;
                    // Utiliser le dispatcher de manière sécurisée
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => Progress = progress);

                    // Vérifier régulièrement si un arrêt a été demandé
                    if (StopRequested)
                    {
                        _loggerService.Log($"Copie de {sourceFile} interrompue par l'utilisateur");
                        return;
                    }
                }
            }

            if (!StopRequested)
            {
                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    (currentIndex + 1) * 100.0 / totalFiles,
                    filesRemaining: totalFiles - (currentIndex + 1)
                );

                _loggerService.LogFileTransfer(Name, sourceFile, targetFile, fileInfo.Length, 0);
            }
        }

        // Modifiez également ExecuteDifferentialBackup de la même manière
        private void ExecuteDifferentialBackup(CancellationToken cancellationToken)
        {
            DateTime lastBackupDate = GetLastBackupDate();
            var modifiedFiles = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories)
                                            .Where(f => File.GetLastWriteTime(f) > lastBackupDate)
                                            .ToList();

            long totalSize = modifiedFiles.Sum(f => new FileInfo(f).Length);
            int totalFiles = modifiedFiles.Count;

            _stateService.UpdateState(Name, "InProgress", 0, filesRemaining: totalFiles, totalFiles: totalFiles, totalSize: totalSize);

            for (int i = 0; i < modifiedFiles.Count && !StopRequested; i++)
            {
                if (StopRequested)
                {
                    _loggerService.Log($"Job {Name} arrêté par l'utilisateur avant fichier {i + 1}/{totalFiles}");
                    return;
                }

                while (IsPaused && !StopRequested)
                {
                    Thread.Sleep(500);
                }

                if (StopRequested) return;

                string sourceFile = modifiedFiles[i];
                string relativePath = sourceFile.Substring(SourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                CopyFileWithProgress(sourceFile, targetFile, i, totalFiles, totalSize);

                if (i < modifiedFiles.Count - 1 && !StopRequested)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private DateTime GetLastBackupDate()
        {
            return Directory.Exists(TargetPath)
                ? Directory.GetLastWriteTime(TargetPath)
                : DateTime.MinValue;
        }
    }
}
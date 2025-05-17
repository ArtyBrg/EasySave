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
using CryptoSoft;

namespace EasySave.ViewModels
{
    public class BackupJobViewModel : ViewModelBase
    {
        private readonly BackupJob _backupJob;
        private readonly FileSystemService _fileSystemService;
        private readonly LoggerService _loggerService;
        private readonly StateService _stateService;
        private readonly string encryptionKey = "crypto123";

        private bool _isRunning;
        private double _progress;

        private bool _isPaused;
        private bool _stopRequested; 
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private bool _isEncryptionEnabled;
        public bool IsEncryptionEnabled
        {
            get => _isEncryptionEnabled;
            set => SetProperty(ref _isEncryptionEnabled, value);
        }

        public BackupJobViewModel()
    : this(
        new BackupJob
        {
            Name = "DefaultJob",
            SourcePath = @"C:\Source",
            TargetPath = @"C:\Target",
            Type = "Complete"
        },
        new FileSystemService(new LoggerService()),
        new LoggerService(),
        new StateService(new LoggerService()))
        {
            // Ce constructeur appelle le principal avec tous les arguments nécessaires
        }




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
                StopRequested = true; 
                _loggerService.Log($"Job {Name} stop requested");
                
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

            var settings = SettingsService.Load();
            _loggerService.SetLogFormat(settings.LogFormat);

            IsRunning = true;
            IsPaused = false;
            Progress = 0;
            StopRequested = false; 

            try
            {
                _loggerService.Log($"Starting backup job {Id} - {Name}");
                _stateService.UpdateState(Name, "Active", 0, SourcePath, TargetPath);

                
                await Task.Run(() =>
                {
                    try
                    {

                        if (Type == "Complete")
                            ExecuteCompleteBackup(cancellationToken);
                        else
                            ExecuteDifferentialBackup(cancellationToken);

                        ExecuteCompleteBackup(files, totalSize, settings.ExtensionsToCrypt);

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

        private void ExecuteCompleteBackup(List<string> files, long totalSize, List<string> extensionsToCrypt)
        {
            _loggerService.Log($"Found {files.Count} files to backup");
           


            for (int i = 0; i < allFiles.Count && !StopRequested; i++) 
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

                string sourceFile = files[i];

                string relativePath = sourceFile[SourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                var extension = Path.GetExtension(sourceFile).ToLower();
                IsEncryptionEnabled = extensionsToCrypt.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase));


                if (IsEncryptionEnabled)
                {
                    targetFile += ".crypt";
                }

                double progressValue = (i * 100.0) / files.Count;
                Progress = progressValue;

                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    progressValue,
                    sourceFile,
                    targetFile,
                    files.Count,
                    totalSize,
                    files.Count - i
                );


                string sourceFile = allFiles[i];
                string relativePath = sourceFile.Substring(SourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                CopyFileWithProgress(sourceFile, targetFile, i, totalFiles, totalSize);

                if (i < allFiles.Count - 1 && !StopRequested)
                {
                    Thread.Sleep(1000); 
                }
            }
        }


        
        private void CopyFileWithProgress(string sourceFile, string targetFile, int currentIndex, int totalFiles, long totalSize)
        {
            var targetDir = Path.GetDirectoryName(targetFile);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }


        private void ExecuteDifferentialBackup()
        {
            DateTime lastBackup = GetLastCompleteBackupDate();
            _loggerService.Log($"Last complete backup was at {lastBackup}");
            _loggerService.Log($"Chiffrement: {(IsEncryptionEnabled ? "Activé" : "Désactivé")}");

            var modifiedFiles = _fileSystemService.GetModifiedFilesSince(SourcePath, lastBackup).ToList();
            long totalSize = modifiedFiles.Sum(f => new FileInfo(f).Length);


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

                string sourceFile = modifiedFiles[i];
                string relativePath = sourceFile[SourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                if (IsEncryptionEnabled)
                {
                    targetFile += ".crypt";
                }

                double progressValue = (i * 100.0) / modifiedFiles.Count;
                Progress = progressValue;

                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    progressValue,
                    sourceFile,
                    targetFile,
                    modifiedFiles.Count,
                    totalSize,
                    modifiedFiles.Count - i
                );


                    double progress = (currentIndex + (totalBytesRead / (double)fileInfo.Length)) * 100.0 / totalFiles;
                   
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => Progress = progress);

                    
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


        private void ExecuteDifferentialBackup(CancellationToken cancellationToken)


        private double CopyAndLog(string sourceFile, string targetFile)

        {
            DateTime lastBackupDate = GetLastBackupDate();
            var modifiedFiles = Directory.GetFiles(SourcePath, "*", SearchOption.AllDirectories)
                                            .Where(f => File.GetLastWriteTime(f) > lastBackupDate)
                                            .ToList();


            long totalSize = modifiedFiles.Sum(f => new FileInfo(f).Length);
            int totalFiles = modifiedFiles.Count;

            _stateService.UpdateState(Name, "InProgress", 0, filesRemaining: totalFiles, totalFiles: totalFiles, totalSize: totalSize);

            for (int i = 0; i < modifiedFiles.Count && !StopRequested; i++)

              Stopwatch sw = Stopwatch.StartNew();

            try

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
                
                _fileSystemService.CopyFile(sourceFile, targetFile);
                sw.Stop(); ;

                

                if (IsEncryptionEnabled)
                {
                    // Crée une copie temporaire avec l'extension .crypt directement dans le dossier cible
                    string targetFileCrypt = targetFile;
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFileCrypt)!);
                    File.Copy(sourceFile, targetFileCrypt, overwrite: true);

                    var fileManager = new CryptoSoft.FileManager(targetFileCrypt, encryptionKey);
                    timeMs = fileManager.TransformFile(); // Chiffre le fichier sur place

                    // Supprimer l'original non chiffré si présent
                    if (File.Exists(targetFile) && !targetFile.EndsWith(".crypt"))
                        File.Delete(targetFile);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                    File.Copy(sourceFile, targetFile, overwrite: true);
                    sw.Stop();
                    timeMs = sw.Elapsed.TotalMilliseconds;
                }

                fileSize = new FileInfo(targetFile).Length;


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


        public void DecryptFile(string encryptedFilePath, string? outputFilePath = null)
        {
            try
            {
                var fileManager = new CryptoSoft.FileManager(encryptedFilePath, encryptionKey);
                fileManager.TransformFile(); // Déchiffre sur place

                string restoredPath = outputFilePath ?? encryptedFilePath.Replace(".crypt", "");

                // Renomme le fichier déchiffré sans l'extension .crypt
                if (File.Exists(restoredPath))
                    File.Delete(restoredPath); // Supprimer s'il existe déjà pour éviter conflit

                File.Move(encryptedFilePath, restoredPath);

                _loggerService.Log($"Déchiffrement réussi : {encryptedFilePath} -> {restoredPath}");
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Erreur de déchiffrement : {ex.Message}");
            }
        }



        private DateTime GetLastCompleteBackupDate()

        {
            return Directory.Exists(TargetPath)
                ? Directory.GetLastWriteTime(TargetPath)
                : DateTime.MinValue;
        }
    }
}
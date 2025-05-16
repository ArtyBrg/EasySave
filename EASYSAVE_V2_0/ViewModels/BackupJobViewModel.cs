using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
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
            _backupJob = backupJob;
            _fileSystemService = fileSystemService;
            _loggerService = loggerService;
            _stateService = stateService;

            ExecuteCommand = new RelayCommand(async param => await ExecuteAsync(), param => !IsRunning);
        }

        // Expose les propriétés du modèle
        public int Id => _backupJob.Id;
        public string Name => _backupJob.Name;
        public string SourcePath => _backupJob.SourcePath;
        public string TargetPath => _backupJob.TargetPath;
        public string Type => _backupJob.Type;

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

        public RelayCommand ExecuteCommand { get; }

        public async Task ExecuteAsync()
        {
            if (IsRunning)
                return;

            var settings = SettingsService.Load();
            _loggerService.SetLogFormat(settings.LogFormat);

            IsRunning = true;
            Progress = 0;

            try
            {
                _loggerService.Log($"Starting backup job {Id} - {Name}");

                await Task.Run(() =>
                {
                    var files = _fileSystemService.GetAllFiles(SourcePath).ToList();
                    long totalSize = _fileSystemService.GetDirectorySize(SourcePath);

                    _stateService.UpdateState(Name, "Active", 0, SourcePath, TargetPath, files.Count, totalSize, files.Count);

                    if (Type == "Complete")
                    {
                        ExecuteCompleteBackup(files, totalSize, settings.ExtensionsToCrypt);
                    }
                    else if (Type == "Differential")
                    {
                        ExecuteDifferentialBackup();
                    }

                    _stateService.UpdateState(Name, "Completed", 100, "", "", files.Count, totalSize, 0);
                    _loggerService.Log($"Backup job {Name} completed successfully");
                });

                Progress = 100;
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Error in backup job {Name}: {ex.Message}");
                _stateService.UpdateState(Name, "Failed", 0);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void ExecuteCompleteBackup(List<string> files, long totalSize, List<string> extensionsToCrypt)
        {
            _loggerService.Log($"Found {files.Count} files to backup");
            //_loggerService.Log($"Chiffrement: {(IsEncryptionEnabled ? "Activé" : "Désactivé")}");

            for (int i = 0; i < files.Count; i++)
            {
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

                double transferTime = CopyAndLog(sourceFile, targetFile);

                double newProgress = (i + 1) * 100.0 / files.Count;
                Progress = newProgress;

                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    newProgress,
                    "",
                    "",
                    files.Count,
                    totalSize,
                    files.Count - (i + 1)
                );
            }
        }


        private void ExecuteDifferentialBackup()
        {
            DateTime lastBackup = GetLastCompleteBackupDate();
            _loggerService.Log($"Last complete backup was at {lastBackup}");
            _loggerService.Log($"Chiffrement: {(IsEncryptionEnabled ? "Activé" : "Désactivé")}");

            var modifiedFiles = _fileSystemService.GetModifiedFilesSince(SourcePath, lastBackup).ToList();
            long totalSize = modifiedFiles.Sum(f => new FileInfo(f).Length);

            _loggerService.Log($"Found {modifiedFiles.Count} modified files to backup");

            for (int i = 0; i < modifiedFiles.Count; i++)
            {
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

                double transferTime = CopyAndLog(sourceFile, targetFile);

                double newProgress = (i + 1) * 100.0 / modifiedFiles.Count;
                Progress = newProgress;

                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    newProgress,
                    "",
                    "",
                    modifiedFiles.Count,
                    totalSize,
                    modifiedFiles.Count - (i + 1)
                );
            }
        }


        private double CopyAndLog(string sourceFile, string targetFile)
        {
            double timeMs = -1;
            long fileSize = 0;

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

                _fileSystemService.CopyFile(sourceFile, targetFile);
                sw.Stop(); ;

                //File.Copy(sourceFile, targetFile, overwrite: true);

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

                _loggerService.LogFileTransfer(
                    backupName: Name,
                    sourcePath: Path.GetFullPath(sourceFile),
                    targetPath: Path.GetFullPath(targetFile),
                    size: fileSize,
                    transferTimeMs: timeMs
                );
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Failed to copy file {sourceFile}: {ex.Message}");

                _loggerService.LogFileTransfer(
                    backupName: Name,
                    sourcePath: Path.GetFullPath(sourceFile),
                    targetPath: Path.GetFullPath(targetFile),
                    size: 0,
                    transferTimeMs: -1
                );
            }

            return timeMs;
        }


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
            // Implémentation simplifiée - à adapter selon vos besoins
            return DateTime.Now.AddDays(-1);
        }
    }
}
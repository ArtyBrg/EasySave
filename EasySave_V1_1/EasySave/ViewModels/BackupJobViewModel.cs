using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using EasySave.Models;
using EasySave.Services;

namespace EasySave.ViewModels
{
    // ViewModel for a backup job (BackupJob)
    public class BackupJobViewModel
    {
        private readonly BackupJob _backupJob;
        private readonly FileSystemService _fileSystemService;
        private readonly LoggerService _loggerService;
        private readonly StateService _stateService;

        // Initializes a new instance of the class
        public BackupJobViewModel(BackupJob backupJob,
                                  FileSystemService fileSystemService,
                                  LoggerService loggerService,
                                  StateService stateService)
        {
            _backupJob = backupJob;
            _fileSystemService = fileSystemService;
            _loggerService = loggerService;
            _stateService = stateService;
        }

        // Gets the backup job model
        public BackupJob Job => _backupJob;

        // Backup job identifier
        public int Id => _backupJob.Id;
        // Backup job name
        public string Name => _backupJob.Name;
        // Source directory of the backup
        public string SourcePath => _backupJob.SourcePath;
        // Target directory of the backup
        public string TargetPath => _backupJob.TargetPath;
        // Type of backup (complete or differential)
        public string Type => _backupJob.Type;

        // Executes the backup job
        public void Execute()
        {
            try
            {
                _loggerService.Log($"Starting backup job {Id} - {Name}");

                var files = _fileSystemService.GetAllFiles(SourcePath).ToList();
                long totalSize = _fileSystemService.GetDirectorySize(SourcePath);

                _stateService.UpdateState(Name, "Active", 0, SourcePath, TargetPath, files.Count, totalSize, files.Count);

                if (Type == "Complete")
                {
                    ExecuteCompleteBackup(files, totalSize);
                }
                else if (Type == "Differential")
                {
                    ExecuteDifferentialBackup();
                }

                _stateService.UpdateState(Name, "Completed", 100, "", "", files.Count, totalSize, 0);
                _loggerService.Log($"Backup job {Name} completed successfully");
            }
            catch (Exception ex)
            {
                _loggerService.LogError($"Error in backup job {Name}: {ex.Message}");
                _stateService.UpdateState(Name, "Failed", 0);
                throw;
            }
        }

        // Executes a complete backup by copying all files from the source to the destination
        private void ExecuteCompleteBackup(List<string> files, long totalSize)
        {
            _loggerService.Log($"Found {files.Count} files to backup");

            for (int i = 0; i < files.Count; i++)
            {
                string sourceFile = files[i];
                string relativePath = sourceFile[SourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                // Updates the backup state
                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    (i * 100.0) / files.Count,
                    files[i],
                    targetFile,
                    files.Count,
                    totalSize,
                    files.Count - i
                );

                double transferTime = CopyAndLog(sourceFile, targetFile); // Copies the file and logs the transfer

                double progress = (i + 1) * 100.0 / files.Count; // Updates the backup progress
                // Updates the backup state
                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    progress,
                    "",
                    "",
                    files.Count,
                    totalSize,
                    files.Count - (i + 1)
                );
            }
        }

        // Executes a differential backup by copying only the files modified since the last complete backup
        private void ExecuteDifferentialBackup()
        {
            DateTime lastBackup = GetLastCompleteBackupDate();
            _loggerService.Log($"Last complete backup was at {lastBackup}");

            var modifiedFiles = _fileSystemService.GetModifiedFilesSince(SourcePath, lastBackup).ToList();
            long totalSize = modifiedFiles.Sum(f => new FileInfo(f).Length);

            _loggerService.Log($"Found {modifiedFiles.Count} modified files to backup");

            for (int i = 0; i < modifiedFiles.Count; i++)
            {
                string sourceFile = modifiedFiles[i];
                string relativePath = sourceFile[SourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    (i * 100.0) / modifiedFiles.Count,
                    modifiedFiles[i],
                    targetFile,
                    modifiedFiles.Count,
                    totalSize,
                    modifiedFiles.Count - i
                );

                double transferTime = CopyAndLog(sourceFile, targetFile);

                double progress = (i + 1) * 100.0 / modifiedFiles.Count;
                _stateService.UpdateState(
                    Name,
                    "InProgress",
                    progress,
                    "",
                    "",
                    modifiedFiles.Count,
                    totalSize,
                    modifiedFiles.Count - (i + 1)
                );
            }
        }

        // Copies a file and logs the transfer with its time and size
        private double CopyAndLog(string sourceFile, string targetFile)
        {
            double timeMs = -1;
            long fileSize = 0;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

                Stopwatch sw = Stopwatch.StartNew();
                _fileSystemService.CopyFile(sourceFile, targetFile);
                sw.Stop();

                timeMs = sw.Elapsed.TotalMilliseconds;
                fileSize = new FileInfo(sourceFile).Length;

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

        // Gets the date of the last complete backup
        private DateTime GetLastCompleteBackupDate()
        {
            // Simplified implementation - adapt as needed
            return DateTime.Now.AddDays(-1);
        }
    }
}

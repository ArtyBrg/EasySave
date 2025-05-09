using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LoggerLib;
using System.Diagnostics;

// Travail de sauvegarde

namespace EasySave
{
    public class BackupJob
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string SourcePath { get; set; }
        public required string TargetPath { get; set; }
        public required string Type { get; set; } // "Complete" ou "Differential"

        private readonly DailyJsonLogger jsonLogger = new DailyJsonLogger("Logs"); // le dossier où seront les logs

        public void Execute()
        {
            try
            {
                Logger.Log($"Starting backup job {Id} - {Name}");

                var files = FileSystem.GetAllFiles(SourcePath).ToList();
                long totalSize = FileSystem.GetDirectorySize(SourcePath);

                StateWriter.UpdateState(Name, "Active", 0, SourcePath, TargetPath, files.Count, totalSize, files.Count);

                if (Type == "Complete")
                {
                    ExecuteCompleteBackup(files, totalSize);
                }
                else if (Type == "Differential")
                {
                    ExecuteDifferentialBackup();
                }

                StateWriter.UpdateState(Name, "Completed", 100, "", "", files.Count, totalSize, 0);
                Logger.Log($"Backup job {Name} completed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in backup job {Name}: {ex.Message}");
                StateWriter.UpdateState(Name, "Failed", 0);
                throw;
            }
        }

        private void ExecuteCompleteBackup(List<string> files, long totalSize)
        {
            Logger.Log($"Found {files.Count} files to backup");

            for (int i = 0; i < files.Count; i++)
            {
                string sourceFile = files[i];
                string relativePath = sourceFile[SourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);
              
                StateWriter.UpdateState(
                    Name,
                    "InProgress",
                    (i * 100.0) / files.Count,
                    files[i],
                    targetFile,
                    files.Count,
                    totalSize,
                    files.Count - i
                );

                FileSystem.CopyFile(files[i], targetFile);
                double transferTime = CopyAndLog(sourceFile, targetFile);

                double progress = (i + 1) * 100.0 / files.Count;
                StateWriter.UpdateState(
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

        private void ExecuteDifferentialBackup()
        {
            DateTime lastBackup = GetLastBackupDate();
            Logger.Log($"Last complete backup was at {lastBackup}");

            var modifiedFiles = FileSystem.GetModifiedFilesSince(SourcePath, lastBackup).ToList();
            long totalSize = modifiedFiles.Sum(f => new FileInfo(f).Length);

            Logger.Log($"Found {modifiedFiles.Count} modified files to backup");

            for (int i = 0; i < modifiedFiles.Count; i++)
            {
                string sourceFile = modifiedFiles[i];
                string relativePath = sourceFile[SourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                StateWriter.UpdateState(
                    Name,
                    "InProgress",
                    (i * 100.0) / modifiedFiles.Count,
                    modifiedFiles[i],
                    targetFile,
                    modifiedFiles.Count,
                    totalSize,
                    modifiedFiles.Count - i
                );

                FileSystem.CopyFile(modifiedFiles[i], targetFile);
              
                double transferTime = CopyAndLog(sourceFile, targetFile);

                double progress = (i + 1) * 100.0 / modifiedFiles.Count;
                StateWriter.UpdateState(
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

        private double CopyAndLog(string sourceFile, string targetFile)
        {
            double timeMs = -1;
            long fileSize = 0;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

                Stopwatch sw = Stopwatch.StartNew();
                FileSystem.CopyFile(sourceFile, targetFile);
                sw.Stop();

                timeMs = sw.Elapsed.TotalMilliseconds;
                fileSize = new FileInfo(sourceFile).Length;

                jsonLogger.LogFileTransfer(
                    backupName: Name,
                    sourcePath: Path.GetFullPath(sourceFile),
                    targetPath: Path.GetFullPath(targetFile),
                    size: fileSize,
                    transferTimeMs: timeMs
                );
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to copy file {sourceFile}: {ex.Message}");

                jsonLogger.LogFileTransfer(
                    backupName: Name,
                    sourcePath: Path.GetFullPath(sourceFile),
                    targetPath: Path.GetFullPath(targetFile),
                    size: 0,
                    transferTimeMs: -1
                );
            }

            return timeMs;
        }

        private DateTime GetLastCompleteBackupDate()
        {
            // Implémentation simplifiée - à adapter selon vos besoins
            return DateTime.Now.AddDays(-1);
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public void Execute()
        {
            try
            {
                Logger.Log($"Starting backup job {Id} - {Name}");
                StateWriter.UpdateState(Name, "Active", 0);

                if (Type == "Complete")
                {
                    ExecuteCompleteBackup();
                    UpdateLastCompleteBackupDate();
                }
                else if (Type == "Differential")
                {
                    ExecuteDifferentialBackup();
                }
                else
                {
                    throw new InvalidOperationException($"Unknown backup type: {Type}");
                }

                StateWriter.UpdateState(Name, "Completed", 100);
                Logger.Log($"Backup job {Name} completed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error in backup job {Name}: {ex.Message}");
                StateWriter.UpdateState(Name, "Failed", 0);
                throw;
            }
        }

        private void ExecuteCompleteBackup()
        {
            var files = FileSystem.GetAllFiles(SourcePath).ToList();
            Logger.Log($"Found {files.Count} files to backup");

            for (int i = 0; i < files.Count; i++)
            {
                string relativePath = files[i][SourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                FileSystem.CopyFile(files[i], targetFile);

                double progress = (i + 1) * 100.0 / files.Count;
                StateWriter.UpdateState(Name, "InProgress", progress);
            }
        }

        private void ExecuteDifferentialBackup()
        {
            DateTime lastCompleteBackup = GetLastCompleteBackupDate();
            Logger.Log($"Last complete backup was at {lastCompleteBackup}");

            var modifiedFiles = FileSystem.GetModifiedFilesSince(SourcePath, lastCompleteBackup).ToList();
            Logger.Log($"Found {modifiedFiles.Count} modified files to backup");

            for (int i = 0; i < modifiedFiles.Count; i++)
            {
                string relativePath = modifiedFiles[i][SourcePath.Length..].TrimStart(Path.DirectorySeparatorChar);
                string targetFile = Path.Combine(TargetPath, relativePath);

                FileSystem.CopyFile(modifiedFiles[i], targetFile);

                double progress = (i + 1) * 100.0 / modifiedFiles.Count;
                StateWriter.UpdateState(Name, "InProgress", progress);
            }
        }

        private DateTime GetLastCompleteBackupDate()
        {
            const string backupDateFile = "last_complete_backup.txt";

            if (File.Exists(backupDateFile))
            {
                try
                {
                    return DateTime.Parse(File.ReadAllText(backupDateFile));
                }
                catch
                {
                    Logger.LogError("Failed to read last backup date, using default");
                }
            }
            return DateTime.MinValue;
        }

        private void UpdateLastCompleteBackupDate()
        {
            const string backupDateFile = "last_complete_backup.txt";
            try
            {
                File.WriteAllText(backupDateFile, DateTime.Now.ToString("O"));
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to update last backup date: {ex.Message}");
            }
        }
    }
}

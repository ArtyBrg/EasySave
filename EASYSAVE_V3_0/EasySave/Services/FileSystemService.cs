using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasySave.Models;
using EasySave.Services;
using System.Threading.Tasks;


namespace EasySave.Services
{
    // Manages file system operations such as getting files, copying files, and calculating directory size.
    public class FileSystemService
    {

        // Logger service for logging operations
        private readonly LoggerService _logger;

        public FileSystemService(LoggerService logger)
        {
            _logger = logger;
        }

        public bool CanBackupNonPriorityFile(List<BackupJob> allJobs, List<string> priorityExtensions)
        {
            // Vérifier si des fichiers prioritaires sont en attente sur tous les jobs
            return !allJobs.Any(job => job.HasPendingPriorityFiles(priorityExtensions));
        }

        // Gets all files in the specified directory and its subdirectories.
        public IEnumerable<string> GetAllFiles(string path)
        {
            // Validate the path
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            // Check if the directory exists
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }

        // Gets all files in the specified directory and its subdirectories that have been modified since the given date.
        public IEnumerable<string> GetModifiedFilesSince(string path, DateTime since)
        {
            return GetAllFiles(path).Where(f => File.GetLastWriteTime(f) > since);
        }

        // Gets all files in the specified directory and its subdirectories that are larger than the given size.
        public void CopyFile(string source, string target)
        {
            // Validate the source and target paths
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Source path cannot be null or empty", nameof(source));

            // Check if the source file exists
            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target path cannot be null or empty", nameof(target));

            // Check if the target directory exists
            if (!File.Exists(source))
                throw new FileNotFoundException($"Source file not found: {source}");

            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Copy(source, target, true);
            _logger.Log($"File copied from {source} to {target}");
        }

        // Gets all files in the specified directory and its subdirectories that are larger than the given size.
        public long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                          .Sum(file => new FileInfo(file).Length);
        }


    }
}
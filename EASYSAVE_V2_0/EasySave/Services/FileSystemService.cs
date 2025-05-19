using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasySave.Services;

namespace EasySave.Services
{
    public class FileSystemService
    {
        private readonly LoggerService _logger;

        public FileSystemService(LoggerService logger)
        {
            _logger = logger;
        }

        public IEnumerable<string> GetAllFiles(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }

        public IEnumerable<string> GetModifiedFilesSince(string path, DateTime since)
        {
            return GetAllFiles(path).Where(f => File.GetLastWriteTime(f) > since);
        }

        public void CopyFile(string source, string target)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Source path cannot be null or empty", nameof(source));

            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target path cannot be null or empty", nameof(target));

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

        public long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                          .Sum(file => new FileInfo(file).Length);
        }
    }
}
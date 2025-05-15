using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Gestionnaire de fichiers

namespace EasySave
{
    public static class FileSystem
    {
        public static IEnumerable<string> GetAllFiles(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }

        public static IEnumerable<string> GetModifiedFilesSince(string path, DateTime since)
        {
            return GetAllFiles(path).Where(f => File.GetLastWriteTime(f) > since);
        }

        public static void CopyFile(string source, string target)
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
            Logger.Log($"File copied from {source} to {target}");
        }

        public static long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                          .Sum(file => new FileInfo(file).Length);
        }
    }
}

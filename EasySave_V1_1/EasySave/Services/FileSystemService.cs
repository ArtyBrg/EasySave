//Services/FileSystemService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using EasySave.Services;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EasySave.Services
{
    //Recovering, copying and analysing files and folders
    public class FileSystemService
    {
        private readonly LoggerService _logger;

        public FileSystemService(LoggerService logger)
        {
            _logger = logger;
        }

        //Recursive retrieval of all files in a folder
        public IEnumerable<string> GetAllFiles(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            // Recover files in the folder and sub-folders
            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }

        //Recover files modified in a folder (since a certain date)
        public IEnumerable<string> GetModifiedFilesSince(string path, DateTime since)
        {
            //Filter files by last modified date
            return GetAllFiles(path).Where(f => File.GetLastWriteTime(f) > since);
        }

        //Copy a file from a source to a destination folder
        public void CopyFile(string source, string target)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Source path cannot be null or empty", nameof(source));

            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target path cannot be null or empty", nameof(target));

            if (!File.Exists(source))
                throw new FileNotFoundException($"Source file not found: {source}");

            // Create a folder if necessary
            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Copy(source, target, true);
            _logger.Log($"File copied from {source} to {target}");
        }

        //Calculates the total size (in bytes) of a folder
        public long GetDirectorySize(string path)
        {
            if (!Directory.Exists(path))
                return 0;

            // Add up the size of all the files in the folder and sub-folders
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                          .Sum(file => new FileInfo(file).Length);
        }
    }
}
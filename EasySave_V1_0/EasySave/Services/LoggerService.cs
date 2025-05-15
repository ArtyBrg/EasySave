using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Models;

namespace EasySave.Services
{
    public class LoggerService
    {
        private readonly string _logDirectory;

        public LoggerService(string logDirectory = "Logs")
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        public void Log(string message)
        {
            Console.WriteLine($"[LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}");
        }

        public void LogError(string errorMessage)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {errorMessage}");
        }

        public void LogFileTransfer(string backupName, string sourcePath, string targetPath, long size, double transferTimeMs)
        {
            var entry = new JsonLogEntry
            {
                Name = backupName,
                FileSource = sourcePath,
                FileTarget = targetPath,
                FileSize = size,
                FileTransferTime = transferTimeMs,
                Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            LogJsonEntry(entry);
        }

        private void LogJsonEntry(JsonLogEntry entry)
        {
            string logFilePath = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");

            List<JsonLogEntry> logEntries = new();

            if (File.Exists(logFilePath))
            {
                string existingContent = File.ReadAllText(logFilePath);
                try
                {
                    logEntries = JsonSerializer.Deserialize<List<JsonLogEntry>>(existingContent) ?? new();
                }
                catch
                {
                    // fichier mal formé → réinitialisation
                    logEntries = new();
                }
            }

            logEntries.Add(entry);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            File.WriteAllText(logFilePath, JsonSerializer.Serialize(logEntries, options));
        }
    }
}
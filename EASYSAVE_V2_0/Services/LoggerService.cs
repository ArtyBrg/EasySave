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

        public event EventHandler<string> LogMessageAdded;

        public LoggerService(string logDirectory = "Logs")
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
            LoadExistingLogs();
        }

        private void LoadExistingLogs()
        {
            try
            {
                string logFilePath = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");
                if (File.Exists(logFilePath))
                {
                    string existingContent = File.ReadAllText(logFilePath);
                    if (!string.IsNullOrWhiteSpace(existingContent))
                    {
                        LogMessageAdded?.Invoke(this, $"Loaded existing logs from {logFilePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessageAdded?.Invoke(this, $"[ERROR] Failed to load existing logs: {ex.Message}");
            }
        }

        public void Log(string message)
        {
            var logMessage = $"[LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
            LogMessageAdded?.Invoke(this, logMessage);
        }

        public void LogError(string errorMessage)
        {
            var logMessage = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {errorMessage}";
            LogMessageAdded?.Invoke(this, logMessage);
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

            var message = $"[TRANSFER] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {backupName} - {sourcePath} -> {targetPath}";
            LogMessageAdded?.Invoke(this, message);
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
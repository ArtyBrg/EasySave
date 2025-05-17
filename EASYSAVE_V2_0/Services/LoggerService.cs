using System;
using System.IO;
using System.Text.Json;
using LoggerLib; // LoggerLib.dll
using EasySave.Models;

namespace EasySave.Services
{
    public class LoggerService
    {
        private DailyLogger _dailyLogger;
        private string _logDirectory;

        public event EventHandler<string> LogMessageAdded;

        public LoggerService(string logDirectory = "Logs")
        {
            var settings = SettingsService.Load();
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

            _dailyLogger = new DailyLogger(logDirectory, settings.LogFormat);
            Log("Logger service initialized");
        }

        public void SetLogFormat(LogFormat format)
        {
            try
            {
                _dailyLogger.SetLogFormat(format);

                var settings = SettingsService.Load();
                settings.LogFormat = format;
                SettingsService.Save(settings);
            }
            catch (Exception ex)
            {
                LogError($"Failed to set log format: {ex.Message}");
            }
        }

        public void Log(string message)
        {
            string logMessage = $"[LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
            LogMessageAdded?.Invoke(this, logMessage);
        }

        public void LogError(string message)
        {
            string errorMessage = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
            LogMessageAdded?.Invoke(this, errorMessage);
        }

        public void LogFileTransfer(string backupName, string sourcePath, string targetPath, long size, double transferTimeMs)
        {
            try
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
          
                _dailyLogger.LogFileTransfer(
                    backupName,
                    Path.GetFullPath(sourcePath),
                    Path.GetFullPath(targetPath),
                    size,
                    transferTimeMs
                );

                var message = $"[TRANSFER] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {backupName} - {sourcePath} -> {targetPath}";
                Console.WriteLine(message); // Écrire dans la console pour le débogage
                LogMessageAdded?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                LogError($"Failed to log file transfer: {ex.Message}");
            }
        }
    }
}
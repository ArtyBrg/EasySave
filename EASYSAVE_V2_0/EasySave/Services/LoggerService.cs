using System;
using System.IO;
using System.Text.Json;
using LoggerLib; // LoggerLib.dll
using EasySave.Models;

namespace EasySave.Services
{
    //Manages the logging of the application
    public class LoggerService
    {
        private DailyLogger _dailyLogger;
        private string _logDirectory;

        public event EventHandler<string> LogMessageAdded;

        //Constructor for the LoggerService
        public LoggerService(string logDirectory = "Logs")
        {
            var settings = SettingsService.Load();
            _logDirectory = logDirectory;

            Directory.CreateDirectory(_logDirectory);
            LoadExistingLogs();
        }

        //Load existing logs from the log directory
        private void LoadExistingLogs()
        {
            try
            {
                string logFilePath = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.json");
                // Check if the log file exists
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

            var settings = SettingsService.Load();
            _dailyLogger = new DailyLogger(_logDirectory, settings.LogFormat);
            Log("Logger service initialized");
        }

        //Set the log format for the logger
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

        //Log a message to the console and the log file
        public void Log(string message)
        {
            string logMessage = $"[LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
            LogMessageAdded?.Invoke(this, logMessage);
        }

        //Log a message to the console and the log file
        public void LogError(string message)
        {
            string errorMessage = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
            LogMessageAdded?.Invoke(this, errorMessage);
        }

        //Log a file transfer
        public void LogFileTransfer(string backupName, string sourcePath, string targetPath, long size, double transferTimeMs, double encryptionTimeMs)
        {
            try
            {
                _dailyLogger.LogFileTransfer(
                    backupName,
                    Path.GetFullPath(sourcePath),
                    Path.GetFullPath(targetPath),
                    size,
                    transferTimeMs,
                    encryptionTimeMs
                );

                var message = $"[TRANSFER] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {backupName} - {sourcePath} -> {targetPath}";
                Console.WriteLine(message);
                LogMessageAdded?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                LogError($"Failed to log file transfer: {ex.Message}");
            }
        }
    }
}

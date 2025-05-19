//Services/LoggerService.cs
using System;
using System.IO;
using LoggerLib;
using EasySave.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EasySave.Services
{
    // Service for logging actions, errors, and file transfers
    public class LoggerService
    {
        private readonly DailyLogger _dailyLogger; // External logger used to record transfers in a log file

        // Initializes the log service with the format defined in the application settings
        public LoggerService(string logDirectory = "Logs")
        {
            var settings = SettingsService.Load();
            _dailyLogger = new DailyLogger(logDirectory, settings.LogFormat);
        }

        // Dynamically changes the log format (JSON or XML)
        public void SetLogFormat(LogFormat format)
        {
            _dailyLogger.SetLogFormat(format);
            Log($"Log format switched to {format}");

            var settings = SettingsService.Load();
            settings.LogFormat = format;
            SettingsService.Save(settings);
        }

        // Displays a message in the console with a timestamp (not saved in the log)
        public void Log(string message)
        {
            var logMessage = $"[LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
            Console.WriteLine(logMessage);
        }

        // Displays an error in the console in red with a timestamp (not saved in the log)
        public void LogError(string errorMessage)
        {
            var logMessage = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {errorMessage}";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(logMessage);
            Console.ResetColor();
        }

        // Logs a file transfer (backup) via the external logger (also displays in the console)
        public void LogFileTransfer(string backupName, string sourcePath, string targetPath, long size, double transferTimeMs)
        {
            _dailyLogger.LogFileTransfer(
                backupName,
                Path.GetFullPath(sourcePath),
                Path.GetFullPath(targetPath),
                size,
                transferTimeMs
            );

            var message = $"[TRANSFER] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {backupName} - {sourcePath} -> {targetPath}";
            Console.WriteLine(message);
        }
    }
}

using System;
using System.IO;
using LoggerLib;
using EasySave.Models;

namespace EasySave.Services
{
    public class LoggerService
    {
        private readonly DailyLogger _dailyLogger;

        public LoggerService(string logDirectory = "Logs")
        {
            var settings = SettingsService.Load();
            _dailyLogger = new DailyLogger(logDirectory, settings.LogFormat);
        }

        public void SetLogFormat(LogFormat format)
        {
            _dailyLogger.SetLogFormat(format);
            Log($"Log format switched to {format}");

            var settings = SettingsService.Load();
            settings.LogFormat = format;
            SettingsService.Save(settings);
        }

        public void Log(string message)
        {
            var logMessage = $"[LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
            Console.WriteLine(logMessage);
        }

        public void LogError(string errorMessage)
        {
            var logMessage = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {errorMessage}";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(logMessage);
            Console.ResetColor();
        }

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

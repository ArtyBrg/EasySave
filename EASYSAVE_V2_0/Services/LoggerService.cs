using System;
using System.IO;
using LoggerLib; // LoggerLib.dll
using EasySave.Models;

namespace EasySave.Services
{
    public class LoggerService
    {
        private readonly DailyLogger _dailyLogger;

        // Pour notifier l'interface WPF
        public event EventHandler<string> LogMessageAdded;

        public LoggerService(string logDirectory = "Logs", LogFormat format = LogFormat.Json)
        {
            _dailyLogger = new DailyLogger(logDirectory, format);
        }

        /// <summary>
        /// Change dynamiquement le format (JSON ou XML)
        /// </summary>
        public void SetLogFormat(LogFormat format)
        {
            _dailyLogger.SetLogFormat(format);
            Log($"Log format switched to {format}");
        }

        /// <summary>
        /// Écriture d'un message générique dans l'interface
        /// </summary>
        public void Log(string message)
        {
            var logMessage = $"[LOG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
            LogMessageAdded?.Invoke(this, logMessage);
        }

        /// <summary>
        /// Écriture d'un message d'erreur dans l'interface
        /// </summary>
        public void LogError(string errorMessage)
        {
            var logMessage = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {errorMessage}";
            LogMessageAdded?.Invoke(this, logMessage);
        }

        /// <summary>
        /// Écriture d'une ligne de log dans le fichier (et dans l'UI)
        /// </summary>
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
            LogMessageAdded?.Invoke(this, message);
        }
    }
}

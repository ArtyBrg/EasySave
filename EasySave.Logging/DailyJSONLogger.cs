using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoggerLib
{
    public class JsonLogEntry
    {
        public string Name { get; set; }
        public string FileSource { get; set; }
        public string FileTarget { get; set; }
        public long FileSize { get; set; }
        public double FileTransferTime { get; set; }
        public string time { get; set; }
    }

    public class DailyJsonLogger
    {
        private readonly string _logDirectory;

        public DailyJsonLogger(string logDirectory)
        {
            _logDirectory = logDirectory;
            Directory.CreateDirectory(_logDirectory);
        }

        public void Log(JsonLogEntry entry)
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

        public void LogFileTransfer(string backupName, string sourcePath, string targetPath, long size, double transferTimeMs)
        {
            var entry = new JsonLogEntry
            {
                Name = backupName,
                FileSource = sourcePath,
                FileTarget = targetPath,
                FileSize = size,
                FileTransferTime = transferTimeMs,
                time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            Log(entry);
        }
    }
}

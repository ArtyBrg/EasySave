using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace LoggerLib
{
    // Enum for log format options
    public enum LogFormat
    {
        Json,
        Xml
    }

    // Class representing a log entry
    public class JsonLogEntry
    {
        /// Properties for the log entry
        public string Name { get; set; } // Name of the backup job
        public string FileSource { get; set; } // Source file path
        public string FileTarget { get; set; } // Target file path
        public long FileSize { get; set; } // Size of the file in bytes
        public double FileTransferTime { get; set; } // Time taken to transfer the file in milliseconds
        public double EncryptionTime { get; set; } // Time taken to encrypt the file in milliseconds
        public string Time { get; set; } // Timestamp of the log entry
    }

    [XmlRoot("LogEntries")]
    // Class for XML serialization of log entries
    public class XmlLogEntryList
    {
        [XmlElement("LogEntry")]
        // List of log entries
        public List<JsonLogEntry> Entries { get; set; } = new();
    }

    // Class for logging file transfers and errors
    public class DailyLogger
    {
        private readonly string _logDirectory;
        private LogFormat _logFormat;

        // Constructor to initialize the logger with a directory and format
        public DailyLogger(string logDirectory = "Logs", LogFormat logFormat = LogFormat.Json)
        {
            _logDirectory = logDirectory;
            _logFormat = logFormat;
            Directory.CreateDirectory(_logDirectory);
        }

        // Method to set the log format dynamically
        public void SetLogFormat(LogFormat format)
        {
            _logFormat = format;
        }

        // Method to log a file transfer entry
        public void Log(JsonLogEntry entry)
        {
            string extension = _logFormat == LogFormat.Json ? "json" : "xml";
            string logFilePath = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.{extension}");

            if (_logFormat == LogFormat.Json)
            {
                LogJson(entry, logFilePath);
            }
            else
            {
                LogXml(entry, logFilePath);
            }
        }

        // Method to log a message in the console
        private void LogJson(JsonLogEntry entry, string path)
        {
            List<JsonLogEntry> logEntries = new();

            if (File.Exists(path))
            {
                string existingContent = File.ReadAllText(path);
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

            // Serialize the log entries to JSON and write to the file
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            File.WriteAllText(path, JsonSerializer.Serialize(logEntries, options));
        }

        // Method to log an XML entry
        private void LogXml(JsonLogEntry entry, string path)
        {
            XmlLogEntryList logEntries = new();

            if (File.Exists(path))
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Open);
                    var serializer = new XmlSerializer(typeof(XmlLogEntryList));
                    logEntries = (XmlLogEntryList)serializer.Deserialize(stream) ?? new XmlLogEntryList();
                }
                catch
                {
                    logEntries = new XmlLogEntryList();
                }
            }

            logEntries.Entries.Add(entry);

            using var writeStream = new FileStream(path, FileMode.Create);
            var xmlSerializer = new XmlSerializer(typeof(XmlLogEntryList));
            xmlSerializer.Serialize(writeStream, logEntries);
        }

        // Method to log a message in the console
        public void LogFileTransfer(string backupName, string sourcePath, string targetPath, long size, double transferTimeMs, double encryptiontime)
        {
            // Log the file transfer in the console
            var entry = new JsonLogEntry
            {
                Name = backupName,
                FileSource = Path.GetFullPath(sourcePath),
                FileTarget = Path.GetFullPath(targetPath),
                FileSize = size,
                FileTransferTime = transferTimeMs,
                EncryptionTime = encryptiontime,
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Log(entry);
        }
    }
}

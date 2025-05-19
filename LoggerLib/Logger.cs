using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace LoggerLib
{
    // LoggerLib is a library for logging file transfer operations in JSON or XML format.
    public enum LogFormat
    {
        Json,
        Xml
    }

    // JsonLogEntry represents a single log entry for a file transfer operation.
    public class JsonLogEntry
    {
        public string Name { get; set; } // Name of the backup job
        public string FileSource { get; set; } // Source file path
        public string FileTarget { get; set; } // Target file path
        public long FileSize { get; set; } // Size of the file in bytes
        public double FileTransferTime { get; set; } // Time taken to transfer the file in milliseconds
        public string Time { get; set; } // Time of the log entry
        public double EncryptionTime { get; set; } // Time taken to encrypt the file in milliseconds

    }

    [XmlRoot("LogEntries")]
    public class XmlLogEntryList
    {
        [XmlElement("LogEntry")]
        public List<JsonLogEntry> Entries { get; set; } = new();
    }

    // DailyLogger is a class for logging file transfer operations to a daily log file.
    public class DailyLogger
    {
        private readonly string _logDirectory;
        private LogFormat _logFormat;

        // Constructor to initialize the logger with a specified log directory and format.
        public DailyLogger(string logDirectory = "Logs", LogFormat logFormat = LogFormat.Json)
        {
            _logDirectory = logDirectory;
            _logFormat = logFormat;
            Directory.CreateDirectory(_logDirectory);
        }

        // Set the log format (JSON or XML).
        public void SetLogFormat(LogFormat format)
        {
            _logFormat = format;
        }

        // Log a file transfer operation.
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

        // Log a file transfer operation in JSON format.
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

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            File.WriteAllText(path, JsonSerializer.Serialize(logEntries, options));
        }

        // Log a file transfer operation in XML format.
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

        // Log a file transfer operation with additional details.
        public void LogFileTransfer(string backupName, string sourcePath, string targetPath, long size, double transferTimeMs, double encryptionTimeMs)
        {
            var entry = new JsonLogEntry
            {
                Name = backupName,
                FileSource = Path.GetFullPath(sourcePath),
                FileTarget = Path.GetFullPath(targetPath),
                FileSize = size,
                FileTransferTime = transferTimeMs,
                EncryptionTime = encryptionTimeMs,
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            Log(entry);
        }
    }
}

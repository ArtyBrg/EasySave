using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace LoggerLib
{
    public enum LogFormat
    {
        Json,
        Xml
    }

    public class JsonLogEntry
    {
        public string Name { get; set; }
        public string FileSource { get; set; }
        public string FileTarget { get; set; }
        public long FileSize { get; set; }
        public double FileTransferTime { get; set; }
        public string Time { get; set; }
        public double EncryptionTime { get; set; } 

    }

    [XmlRoot("LogEntries")]
    public class XmlLogEntryList
    {
        [XmlElement("LogEntry")]
        public List<JsonLogEntry> Entries { get; set; } = new();
    }

    public class DailyLogger
    {
        private readonly string _logDirectory;
        private LogFormat _logFormat;

        public DailyLogger(string logDirectory = "Logs", LogFormat logFormat = LogFormat.Json)
        {
            _logDirectory = logDirectory;
            _logFormat = logFormat;
            Directory.CreateDirectory(_logDirectory);
        }

        public void SetLogFormat(LogFormat format)
        {
            _logFormat = format;
        }

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

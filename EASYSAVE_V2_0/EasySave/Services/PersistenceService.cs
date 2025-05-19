using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services
{
    public class PersistenceService
    {
        private const string JobsFile = "jobs.json";
        private readonly LoggerService _logger;

        public PersistenceService(LoggerService logger)
        {
            _logger = logger;
        }

        public List<BackupJob> LoadJobs()
        {
            try
            {
                if (File.Exists(JobsFile))
                {
                    var json = File.ReadAllText(JobsFile);
                    return JsonSerializer.Deserialize<List<BackupJob>>(json) ?? new List<BackupJob>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load jobs: {ex.Message}");
            }
            return new List<BackupJob>();
        }

        public void SaveJobs(IEnumerable<BackupJob> jobs)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(JobsFile, JsonSerializer.Serialize(jobs.ToList(), options));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save jobs: {ex.Message}");
            }
        }
    }
}
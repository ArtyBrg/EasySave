using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;

// Gestionnaire de sauvegarde

namespace EasySave
{
    public class BackupManager
    {
        private readonly List<BackupJob> _jobs = new();
        private int _nextId = 1;

        public BackupJob CreateJob(string name, string source, string target, string type)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Job name cannot be null or empty", nameof(name));

            var job = new BackupJob
            {
                Id = _nextId++,
                Name = name,
                SourcePath = Path.GetFullPath(source),
                TargetPath = Path.GetFullPath(target),
                Type = type
            };

            _jobs.Add(job);
            StateWriter.UpdateState(name, "Created", 0);
            Logger.Log($"Created new backup job: {name} (ID: {job.Id})");

            return job;
        }

        public void ExecuteJobs(IEnumerable<int> jobIds)
        {
            foreach (var id in jobIds)
            {
                var job = _jobs.FirstOrDefault(j => j.Id == id);
                if (job != null)
                {
                    try
                    {
                        job.Execute();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to execute backup job {job.Name}: {ex.Message}");
                    }
                }
            }
        }

        public IEnumerable<BackupJob> GetAllJobs() => _jobs.AsReadOnly();
    }
}

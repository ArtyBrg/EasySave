using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Source path cannot be null or empty", nameof(source));

            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("Target path cannot be null or empty", nameof(target));

            if (string.IsNullOrWhiteSpace(type) || (type != "Complete" && type != "Differential"))
                throw new ArgumentException("Type must be either 'Complete' or 'Differential'", nameof(type));

            var job = new BackupJob
            {
                Id = _nextId++,
                Name = name,
                SourcePath = Path.GetFullPath(source),
                TargetPath = Path.GetFullPath(target),
                Type = type
            };

            _jobs.Add(job);
            Logger.Log($"Created new backup job: {name} (ID: {job.Id})");
            StateWriter.UpdateState(name, "Created", 0);

            return job;
        }

        public void ExecuteJobs(IEnumerable<int> jobIds)
        {
            if (jobIds == null)
                throw new ArgumentNullException(nameof(jobIds));

            foreach (var id in jobIds)
            {
                var job = _jobs.FirstOrDefault(j => j.Id == id);
                if (job == null)
                {
                    Logger.LogError($"Backup job with ID {id} not found");
                    continue;
                }

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

        public IEnumerable<BackupJob> GetAllJobs() => _jobs.AsReadOnly();
    }
}

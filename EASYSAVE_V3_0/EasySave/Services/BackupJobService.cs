using EasySave.ViewModels;
using System.Collections.ObjectModel;

namespace EasySave.Services
{
    public class BackupJobService
    {
        private static BackupJobService _instance;
        public static BackupJobService Instance => _instance ??= new BackupJobService(); // Singleton instance

        public ObservableCollection<BackupJobViewModel> Jobs { get; } = new ObservableCollection<BackupJobViewModel>(); // Collection of backup jobs

        // Private constructor to prevent instantiation from outside
        public BackupJobViewModel GetJobByName(string jobName)
        {
            return Jobs.FirstOrDefault(j => j.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase));
        }

        // Adds a new job to the collection
        public void AddJob(BackupJobViewModel job)
        {
            Jobs.Add(job);
        }

        // Removes an existing job in the collection
        public void RemoveJob(string jobName)
        {
            var job = GetJobByName(jobName);
            if (job != null)
            {
                Jobs.Remove(job);
            }
        }

        // Returns the names of all jobs in the collection
        public IEnumerable<string> GetJobNames() 
        {
            return Jobs.Select(j => j.Name);
        }
    }
}
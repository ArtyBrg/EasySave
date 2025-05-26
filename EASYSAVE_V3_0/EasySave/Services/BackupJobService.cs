using EasySave.ViewModels;
using System.Collections.ObjectModel;

namespace EasySave.Services
{
    public class BackupJobService
    {
        private static BackupJobService _instance;
        public static BackupJobService Instance => _instance ??= new BackupJobService();

        public ObservableCollection<BackupJobViewModel> Jobs { get; } = new ObservableCollection<BackupJobViewModel>();

        public BackupJobViewModel GetJobByName(string jobName)
        {
            return Jobs.FirstOrDefault(j => j.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase));
        }

        public void AddJob(BackupJobViewModel job)
        {
            Jobs.Add(job);
        }

        public void RemoveJob(string jobName)
        {
            var job = GetJobByName(jobName);
            if (job != null)
            {
                Jobs.Remove(job);
            }
        }

        public IEnumerable<string> GetJobNames()
        {
            return Jobs.Select(j => j.Name);
        }
    }
}
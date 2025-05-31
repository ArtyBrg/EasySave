using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasySave.ViewModels;
using EasySave.Services;
using System.Threading.Tasks;

namespace EasySave_WPF
{
    // Console application to execute backup jobs
    public class ConsoleApp
    {
        // Entry point for running the console app with job IDs as arguments
        public void Run(string[] args)
        {
            // Initialize required services
            var loggerService = new LoggerService();
            var fileSystemService = new FileSystemService(loggerService);
            var stateService = StateService.Instance;
            var languageService = new LanguageService();
            var persistenceService = new PersistenceService(loggerService);
            languageService.LoadLanguageFromSettings();

            // Create the backup manager view model
            var backupManager = new BackupManagerViewModel(fileSystemService, loggerService, stateService, persistenceService);

            // Parse job IDs from the first argument
            var jobIds = ParseJobIds(args[0]);
            if (!jobIds.Any())
            {
                Console.WriteLine("Aucune Id valides.");
                return;
            }

            // Execute the selected jobs asynchronously
            Task.Run(async () =>
            {
                await backupManager.ExecuteJobsAsync(jobIds);
                Console.WriteLine("Jobs exécutés.");
            }).GetAwaiter().GetResult();
        }

        // Parse job IDs from the command line arguments
        private List<int> ParseJobIds(string input)
        {
            var list = new List<int>();
            // Handle range format (e.g., "1-3")
            if (input.Contains('-'))
            {
                var parts = input.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                    for (int i = start; i <= end; i++) list.Add(i);
            }
            // Handle list format (e.g., "1;2;3")
            else if (input.Contains(';'))
            {
                list.AddRange(input.Split(';').Where(p => int.TryParse(p, out _)).Select(int.Parse));
            }
            // Handle single ID
            else if (int.TryParse(input, out int singleId))
            {
                list.Add(singleId);
            }
            // Return distinct job IDs
            return list.Distinct().ToList();
        }
    }
}


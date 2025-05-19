using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasySave.ViewModels;
using EasySave.Services;
using System.Threading.Tasks;

namespace EasySave_WPF
{
    public class ConsoleApp
    {
        public void Run(string[] args)
        {
            var loggerService = new LoggerService();
            var fileSystemService = new FileSystemService(loggerService);
            var stateService = new StateService(loggerService);
            var languageService = new LanguageService();
            var persistenceService = new PersistenceService(loggerService);
            languageService.LoadLanguageFromSettings();

            var backupManager = new BackupManagerViewModel(fileSystemService, loggerService, stateService, persistenceService);

            var jobIds = ParseJobIds(args[0]);
            if (!jobIds.Any())
            {
                Console.WriteLine("Aucun ID de job valide.");
                return;
            }

            Task.Run(async () =>
            {
                await backupManager.ExecuteJobsAsync(jobIds);
                Console.WriteLine("Jobs exécutés.");
            }).GetAwaiter().GetResult();
        }

        private List<int> ParseJobIds(string input)
        {
            var list = new List<int>();
            if (input.Contains('-'))
            {
                var parts = input.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                    for (int i = start; i <= end; i++) list.Add(i);
            }
            else if (input.Contains(';'))
            {
                list.AddRange(input.Split(';').Where(p => int.TryParse(p, out _)).Select(int.Parse));
            }
            else if (int.TryParse(input, out int singleId))
            {
                list.Add(singleId);
            }
            return list.Distinct().ToList();
        }
    }
}

using System;
using EasySave.Views;
using EasySave.ViewModels;
using EasySave.Services;

namespace EasySave
{

    class Program
    {


        private static List<int> ParseJobIds(string input)
        {
            var jobIds = new List<int>();

            if (input.Contains('-'))
            {
                var parts = input.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    for (int i = start; i <= end; i++)
                        jobIds.Add(i);
                }
            }
            else if (input.Contains(';'))
            {
                var parts = input.Split(';');
                foreach (var part in parts)
                {
                    if (int.TryParse(part, out int id))
                        jobIds.Add(id);
                }
            }
            else if (int.TryParse(input, out int singleId))
            {
                jobIds.Add(singleId);
            }

            return jobIds.Distinct().ToList();
        }

        static void Main(string[] args)
        {
            

            Console.WriteLine("=== EasySave Backup Application ===");
            Console.WriteLine("Version 1.1 MVVM\n");



            try
            {
                // Initialiser les services
                var loggerService = new LoggerService();
                var stateService = new StateService(loggerService);
                var fileSystemService = new FileSystemService(loggerService);
                var languageService = new LanguageService();
                languageService.LoadLanguageFromSettings();

                // Initialiser les ViewModels
                var backupManagerViewModel = new BackupManagerViewModel(fileSystemService, loggerService, stateService);
                var mainViewModel = new MainViewModel(backupManagerViewModel, languageService);

                if (args.Length > 0)
                {
                    Console.WriteLine("Mode automatique avec arguments détecté.");

                    var jobIds = ParseJobIds(args[0]);
                    if (!jobIds.Any())
                    {
                        Console.WriteLine("Aucun ID valide fourni.");
                        return;
                    }

                    try
                    {
                        backupManagerViewModel.ExecuteJobs(jobIds);
                        Console.WriteLine("Sauvegardes exécutées avec succès.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Erreur lors de l'exécution des sauvegardes :");
                        Console.WriteLine(ex.Message);
                    }

                    return; // Sortir proprement
                }

                // Initialiser et démarrer la vue
                var consoleView = new ConsoleView(mainViewModel);
                consoleView.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application crashed: {ex.Message}");
                Console.WriteLine("A critical error occurred. The application will now close.");
                Console.WriteLine("Error details have been logged.");
            }
        }
    }
}
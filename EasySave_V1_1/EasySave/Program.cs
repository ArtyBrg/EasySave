using System;
using EasySave.Views;
using EasySave.ViewModels;
using EasySave.Services;

namespace EasySave
{

    class Program
    {

        // Parses an input string representing job IDs, which can be ranges (e.g., "1-5") or lists separated by semicolons (e.g., "1;2;3")
        private static List<int> ParseJobIds(string input)
        {
            // Checks if the input is empty or null
            var jobIds = new List<int>();

            if (input.Contains('-'))
            {
                // If the input contains a dash, assume it is a range
                var parts = input.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    for (int i = start; i <= end; i++)
                        jobIds.Add(i);
                }
            }
            else if (input.Contains(';'))
            {
                // If the input contains a semicolon, assume it is a list
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

        // Main entry point of the application
        static void Main(string[] args)
        {


            Console.WriteLine("=== EasySave Backup Application ===");
            Console.WriteLine("Version 1.1 MVVM\n");



            try
            {
                // Initialize services
                var loggerService = new LoggerService(); // Logging service
                var stateService = new StateService(loggerService); // Backup state management service
                var fileSystemService = new FileSystemService(loggerService); // File system management service
                var languageService = new LanguageService(); // Language management service
                languageService.LoadLanguageFromSettings(); // Load language from settings

                // Initialize ViewModels
                var backupManagerViewModel = new BackupManagerViewModel(fileSystemService, loggerService, stateService);
                var mainViewModel = new MainViewModel(backupManagerViewModel, languageService);

                if (args.Length > 0)
                {
                    Console.WriteLine("Automatic mode with detected arguments.");

                    var jobIds = ParseJobIds(args[0]);
                    if (!jobIds.Any())
                    {
                        Console.WriteLine("No valid ID provided.");
                        return;
                    }

                    try
                    {
                        backupManagerViewModel.ExecuteJobs(jobIds);
                        Console.WriteLine("Backups executed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while executing backups:");
                        Console.WriteLine(ex.Message);
                    }

                    return;
                }

                // Initialize and start the view
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

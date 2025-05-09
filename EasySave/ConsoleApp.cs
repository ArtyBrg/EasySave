using System;
using System.Linq;

// Application console

namespace EasySave
{
    public class ConsoleApp
    {
        private readonly LanguageManager _languageManager = new();
        private readonly BackupManager _backupManager = new();

        public void Run()
        {
            try
            {
                _languageManager.SetLanguage("EN");
                MainMenuLoop();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Fatal error: {ex.Message}");
                Console.WriteLine("A fatal error occurred. Please check the logs.");
            }
        }

        private void InitializeLanguage()
        {
            Console.WriteLine(_languageManager.GetString("\nTo select your language enter 'FR' or 'EN'"));
            string? input;
            do
            {
                input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine(_languageManager.GetString("LanguageEmptyError"));
                    continue;
                }

                try
                {
                    _languageManager.SetLanguage(input);
                    break;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(_languageManager.GetString("SelectLanguage"));
                }
            } while (true);
        }


        private void MainMenuLoop()
        {
            while (true)
            {
                DisplayMainMenu();
                var input = Console.ReadLine()?.Trim();

                switch (input)
                {
                    case "1":
                        CreateBackupJob();
                        break;
                    case "2":
                        ExecuteBackupJobs();
                        break;
                    case "3":
                        ListBackupJobs();
                        break;
                    case "4":
                        Environment.Exit(0);
                        break;
                    case "5":
                        InitializeLanguage();
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        private void DisplayMainMenu()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_languageManager.GetString("Title"));
            Console.WriteLine(_languageManager.GetString("Main menu create"));
            Console.WriteLine(_languageManager.GetString("Main menu execute"));
            Console.WriteLine(_languageManager.GetString("Main menu list"));
            Console.WriteLine(_languageManager.GetString("Main menu exit"));
            Console.WriteLine(_languageManager.GetString("Main menu select language"));
            Console.WriteLine(_languageManager.GetString("Main menu select option"));

        }

        private void CreateBackupJob()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_languageManager.GetString("Create Backup Job"));

            var existingJobs = _backupManager.GetAllJobs().ToList();
            if (existingJobs.Count >= 5)
            {
                Console.WriteLine(_languageManager.GetString("Maximum job create"));
                return;
            }

            string name = GetUserInput(_languageManager.GetString("Create job name"));
            if (existingJobs.Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine(_languageManager.GetString("Job name already exists"));
                return;
            }
            string source = GetUserInput(_languageManager.GetString("Create job path"));
            string target = GetUserInput(_languageManager.GetString("Create job target"));

            string type;
            do
            {
                type = GetUserInput("Enter backup type (Complete/Differential):").Trim();
                if (type != "Complete" && type != "Differential")
                {
                    Console.WriteLine("Invalid type. Please enter 'Complete' or 'Differential'");
                }
            } while (type != "Complete" && type != "Differential");

            try
            {
                _backupManager.CreateJob(name, source, target, type);
                Console.WriteLine($"Backup job '{name}' created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating backup job: {ex.Message}");
            }
        }

        private void ExecuteBackupJobs()
        {
            Console.WriteLine("\n=== Execute Backup Jobs ===");
            var jobs = _backupManager.GetAllJobs().ToList();

            if (!jobs.Any())
            {
                Console.WriteLine("No backup jobs available.");
                return;
            }

            Console.WriteLine("Available backup jobs:");
            foreach (var job in jobs)
            {
                Console.WriteLine($"{job.Id}: {job.Name} ({job.Type})");
            }

            Console.WriteLine("Enter job IDs to execute (comma separated):");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("No jobs selected.");
                return;
            }

            var jobIds = input.Split(',')
                .Select(idStr => int.TryParse(idStr.Trim(), out int id) ? id : -1)
                .Where(id => id > 0)
                .ToList();

            if (!jobIds.Any())
            {
                Console.WriteLine("No valid job IDs entered.");
                return;
            }

            try
            {
                _backupManager.ExecuteJobs(jobIds);
                Console.WriteLine("Backup jobs execution completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing backup jobs: {ex.Message}");
            }
        }

        private void ListBackupJobs()
        {
            Console.WriteLine("\n=== Backup Jobs List ===");
            var jobs = _backupManager.GetAllJobs().ToList();

            if (!jobs.Any())
            {
                Console.WriteLine("No backup jobs available.");
                return;
            }

            foreach (var job in jobs)
            {
                Console.WriteLine($"ID: {job.Id}");
                Console.WriteLine($"Name: {job.Name}");
                Console.WriteLine($"Source: {job.SourcePath}");
                Console.WriteLine($"Target: {job.TargetPath}");
                Console.WriteLine($"Type: {job.Type}");
                Console.WriteLine();
            }
        }

        private static string GetUserInput(string prompt)
        {
            Console.WriteLine(prompt);
            string? input;
            do
            {
                input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Input cannot be empty. Please try again:");
                }
            } while (string.IsNullOrEmpty(input));

            return input;
        }
    }
}

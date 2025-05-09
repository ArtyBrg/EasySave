using System;
using System.Linq;

// Application console

namespace EasySave
{
    public class ConsoleApp
    {
        private readonly LanguageManager _languagemManager = new();
        private readonly BackupManager _backupManager = new();

        public void Run()
        {
            try
            {
                _languagemManager.SetLanguage("EN");
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
            Console.WriteLine("\n");
            Console.WriteLine(_languagemManager.GetString("Select language"));
            string? input;
            do
            {
                input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine(_languagemManager.GetString("LanguageEmptyError"));
                    continue;
                }

                try
                {
                    _languagemManager.SetLanguage(input);
                    break;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(_languagemManager.GetString("Select language"));
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
            Console.WriteLine(_languagemManager.GetString("Title"));
            Console.WriteLine(_languagemManager.GetString("Main menu create"));
            Console.WriteLine(_languagemManager.GetString("Main menu execute"));
            Console.WriteLine(_languagemManager.GetString("Main menu list"));
            Console.WriteLine(_languagemManager.GetString("Main menu exit"));
            Console.WriteLine(_languagemManager.GetString("Main menu select language"));
            Console.WriteLine(_languagemManager.GetString("Main menu select option"));

        }

        private void CreateBackupJob()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_languagemManager.GetString("Create backup job"));

            var existingJobs = _backupManager.GetAllJobs().ToList();
            if (existingJobs.Count >= 5)
            {
                Console.WriteLine(_languagemManager.GetString("Maximum job create"));
                return;
            }

            string name = GetUserInput(_languagemManager.GetString("Create job name"));
            if (existingJobs.Any(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine(_languagemManager.GetString("Job name already exists"));
                return;
            }

            string source = GetUserInput(_languagemManager.GetString("Create job path"));
            string target = GetUserInput(_languagemManager.GetString("Create job target"));

            string type;
            do
            {
                type = GetUserInput(_languagemManager.GetString("Create job type")).Trim();
                if (type != (_languagemManager.GetString("job type complete")) && type != (_languagemManager.GetString("job type differential")))
                {
                        Console.WriteLine(_languagemManager.GetString("job type error"));

                }
            } while (type != (_languagemManager.GetString("job type complete")) && type != (_languagemManager.GetString("job type differential")));

            if (type == (_languagemManager.GetString("job type complete"))){
                type = "Complete";
            }
            else if (type == (_languagemManager.GetString("job type differential"))){
                type = "Differential";
            };

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

        private static HashSet<int> ParseJobSelection(string input)
        {
            var ids = new HashSet<int>();
            var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (part.Contains('-'))
                {
                    var range = part.Split('-', StringSplitOptions.RemoveEmptyEntries);
                    if (range.Length == 2 &&
                        int.TryParse(range[0], out int start) &&
                        int.TryParse(range[1], out int end))
                    {
                        for (int i = start; i <= end; i++)
                        {
                            ids.Add(i);
                        }
                    }
                }
                else if (int.TryParse(part, out int id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }


        private void ExecuteBackupJobs()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_languagemManager.GetString("Execute backup job"));
            var jobs = _backupManager.GetAllJobs().ToList();

            if (!jobs.Any())
            {
                Console.WriteLine(_languagemManager.GetString("Execute backup no available"));
                return;
            }

            Console.WriteLine(_languagemManager.GetString("Execute backup print list"));
            foreach (var job in jobs)
            {
                Console.WriteLine($"{job.Id}: {job.Name} ({job.Type})");
            }

            Console.WriteLine(_languagemManager.GetString("Execute backup enter job id"));
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine(_languagemManager.GetString("Execute backup no job select"));
                return;
            }

            var selectedIds = ParseJobSelection(input);

            if (!selectedIds.Any())
            {
                Console.WriteLine(_languagemManager.GetString("Execute backup no valid id"));
                return;
            }

            try
            {
                _backupManager.ExecuteJobs(jobIds);
                Console.WriteLine(_languagemManager.GetString("Execute backup execution completed"));
            }
            catch (Exception ex)
            { 
                Console.WriteLine(_languagemManager.GetString("Execute backup executing error"));
                Console.WriteLine($"{ex.Message}");
            }
        }


        private void ListBackupJobs()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_languagemManager.GetString("List job title"));
            var jobs = _backupManager.GetAllJobs().ToList();

            if (!jobs.Any())
            {
                Console.WriteLine(_languagemManager.GetString("List job no backup available"));
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

        private string GetUserInput(string prompt)
        {
            Console.WriteLine(prompt);
            string? input;
            do
            {
                input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine(_languagemManager.GetString("List job input empty"));
                }
            } while (string.IsNullOrEmpty(input));

            return input;
        }
    }
}

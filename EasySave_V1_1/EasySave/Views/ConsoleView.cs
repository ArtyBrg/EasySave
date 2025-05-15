using System;
using System.Collections.Generic;
using System.Linq;
using EasySave.ViewModels;

namespace EasySave.Views
{
    public class ConsoleView
    {
        private readonly MainViewModel _viewModel;

        public ConsoleView(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Run()
        {
            try
            {
                MainMenuLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"A fatal error occurred: {ex.Message}");
                Console.WriteLine("Please check the logs.");
            }
        }

        private void InitializeLanguage()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_viewModel.GetTranslation("Select language"));
            string? input;
            do
            {
                input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine(_viewModel.GetTranslation("LanguageEmptyError"));
                    continue;
                }

                try
                {
                    _viewModel.SetLanguage(input);
                    break;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(_viewModel.GetTranslation("Select language"));
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
            Console.WriteLine(_viewModel.GetTranslation("Title"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu create"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu execute"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu list"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu exit"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu select language"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu select option"));
        }

        private void CreateBackupJob()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_viewModel.GetTranslation("Create backup job"));


            string name = GetUserInput(_viewModel.GetTranslation("Create job name"));
            if (_viewModel.JobNameExists(name))
            {
                Console.WriteLine(_viewModel.GetTranslation("Job name already exists"));
                return;
            }

            string source = GetUserInput(_viewModel.GetTranslation("Create job path"));
            string target = GetUserInput(_viewModel.GetTranslation("Create job target"));

            string type;
            do
            {
                type = GetUserInput(_viewModel.GetTranslation("Create job type")).Trim();
                if (type != (_viewModel.GetTranslation("job type complete")) && type != (_viewModel.GetTranslation("job type differential")))
                {
                    Console.WriteLine(_viewModel.GetTranslation("job type error"));

                }
            } while (type != (_viewModel.GetTranslation("job type complete")) && type != (_viewModel.GetTranslation("job type differential")));

            if (type == (_viewModel.GetTranslation("job type complete")))
            {
                type = "Complete";
            }
            else if (type == (_viewModel.GetTranslation("job type differential")))
            {
                type = "Differential";
            }
            ;

            try
            {
                _viewModel.CreateBackupJob(name, source, target, type);
                Console.WriteLine($"Backup job '{name}' created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating backup job: {ex.Message}");
            }
        }

        private void ExecuteBackupJobs()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_viewModel.GetTranslation("Execute backup job"));
            var jobs = _viewModel.GetAllJobs().ToList();

            if (!jobs.Any())
            {
                Console.WriteLine(_viewModel.GetTranslation("Execute backup no available"));
                return;
            }

            Console.WriteLine(_viewModel.GetTranslation("Execute backup print list"));
            foreach (var job in jobs)
            {
                Console.WriteLine($"{job.Id}: {job.Name} ({job.Type})");
            }

            Console.WriteLine(_viewModel.GetTranslation("Execute backup enter job id"));
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine(_viewModel.GetTranslation("Execute backup no job select"));
                return;
            }

            var selectedIds = ParseJobSelection(input);

            if (!selectedIds.Any())
            {
                Console.WriteLine(_viewModel.GetTranslation("Execute backup no valid id"));
                return;
            }

            try
            {
                _viewModel.ExecuteBackupJobs(selectedIds.ToList());
                Console.WriteLine(_viewModel.GetTranslation("Execute backup execution completed"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(_viewModel.GetTranslation("Execute backup executing error"));
                Console.WriteLine($"{ex.Message}");
            }
        }

        private void ListBackupJobs()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_viewModel.GetTranslation("List job title"));
            var jobs = _viewModel.GetAllJobs().ToList();

            if (!jobs.Any())
            {
                Console.WriteLine(_viewModel.GetTranslation("List job no backup available"));
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

        private string GetUserInput(string prompt)
        {
            Console.WriteLine(prompt);
            string? input;
            do
            {
                input = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine(_viewModel.GetTranslation("List job input empty"));
                }
            } while (string.IsNullOrEmpty(input));

            return input;
        }
    }
}
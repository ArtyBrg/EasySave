using System;
using System.Collections.Generic;
using System.Linq;
using EasySave.Services;
using EasySave.ViewModels;
using LoggerLib;

namespace EasySave.Views
{
    // Console-based view for the EasySave application
    public class ConsoleView
    {
        // ViewModel for managing backup jobs and translations
        private readonly MainViewModel _viewModel;

        // Constructor that initializes the view with the provided ViewModel
        public ConsoleView(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        // Main entry point for the console application
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

        // Initializes the language settings for the application
        private void InitializeLanguage()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_viewModel.GetTranslation("Select language")); // "Select language"
            string? input;
            do
            {
                input = Console.ReadLine()?.Trim();
                // Check if the input is empty or null
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine(_viewModel.GetTranslation("LanguageEmptyError")); // "Language cannot be empty"
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

        // Main loop for the console application
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
                    case "6":
                        SettingsLog();
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        // Displays the main menu options to the user
        private void DisplayMainMenu()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_viewModel.GetTranslation("Title"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu create"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu execute"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu list"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu exit"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu select language"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu select log settings"));
            Console.WriteLine(_viewModel.GetTranslation("Main menu select option"));
        }

        // Prompts the user to create a new backup job
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

        // Prompts the user to execute backup jobs
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

            // Parse the input to get a list of job IDs
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

        // Displays the list of backup jobs to the user
        private void ListBackupJobs()
        {
            Console.WriteLine("\n");
            Console.WriteLine(_viewModel.GetTranslation("List job title"));
            // Get all jobs from the ViewModel
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

        // Parses the user input for job selection
        private static HashSet<int> ParseJobSelection(string input)
        {
            // Split the input by commas and semicolons
            var ids = new HashSet<int>();
            // Split the input by commas and semicolons
            var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (part.Contains('-'))
                {
                    // Handle ranges (e.g., 1-5)
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

        // Prompts the user to select the log format
        private void SettingsLog()
        {
            var settingsVm = new SettingsViewModel();

            Console.WriteLine(_viewModel.GetTranslation("Select Log"));
            string input = Console.ReadLine();

            if (Enum.TryParse(typeof(LogFormat), input, true, out object formatObj) &&
                Enum.IsDefined(typeof(LogFormat), formatObj))
            {
                var format = (LogFormat)formatObj;
                settingsVm.ChangeLogFormat(format);
            }
            else
            {
                Console.WriteLine(_viewModel.GetTranslation("Error Log"));
            }
        }

        // Prompts the user for input and validates it
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
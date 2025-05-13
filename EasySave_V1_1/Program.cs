using System;
using EasySave.Views;
using EasySave.ViewModels;
using EasySave.Services;

namespace EasySave
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== EasySave Backup Application ===");
            Console.WriteLine("Version 1.0 MVVM\n");

            try
            {
                // Initialiser les services
                var loggerService = new LoggerService();
                var stateService = new StateService(loggerService);
                var fileSystemService = new FileSystemService(loggerService);
                var languageService = new LanguageService();

                // Initialiser les ViewModels
                var backupManagerViewModel = new BackupManagerViewModel(fileSystemService, loggerService, stateService);
                var mainViewModel = new MainViewModel(backupManagerViewModel, languageService);

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
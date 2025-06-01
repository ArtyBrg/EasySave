using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using EasySave.Services;
using EasySave.ViewModels;
using EasySave_WPF.Views;

namespace EasySave_WPF
{
    public partial class App : Application
    {
        public static AppViewModel AppViewModel { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create all the directorys needed for the application
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Settings"));
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Logs"));
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "States"));
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "BackupsJobs"));

            // Initialisation des services
            string logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            var loggerService = new LoggerService(logDir);
            var fileSystemService = new FileSystemService(loggerService);
            var stateService = StateService.Instance;
            var languageService = new LanguageService();
            var persistenceService = new PersistenceService(loggerService);

            // Initialisation des ViewModels
            var backupManagerViewModel = new BackupManagerViewModel(fileSystemService, loggerService, stateService, persistenceService);
            var mainViewModel = new MainViewModel(backupManagerViewModel, languageService, loggerService);
            var settingsViewModel = new SettingsViewModel(loggerService);

            // Chargement de la langue des paramètres
            languageService.LoadLanguageFromSettings();

            // TranslationProvider
            AppViewModel = new AppViewModel();

            // Configuration de la fenêtre principale
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
            mainWindow.Show();
        }
    }
}

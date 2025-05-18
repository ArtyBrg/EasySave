using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using EasySave.Services;
using EasySave.ViewModels;

namespace EasySave_WPF
{
    public partial class App : Application
    {
        public static AppViewModel AppViewModel { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialisation des services
            string baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\\..\\..\\"));
            string logDir = Path.Combine(baseDir, "Logs");
            var loggerService = new LoggerService(logDir);
            var fileSystemService = new FileSystemService(loggerService);
            var stateService = new StateService(loggerService);
            var languageService = new LanguageService();
            var persistenceService = new PersistenceService(loggerService);

            // Initialisation des ViewModels
            var backupManagerViewModel = new BackupManagerViewModel(fileSystemService, loggerService, stateService);
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

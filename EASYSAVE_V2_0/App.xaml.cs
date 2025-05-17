using System.Windows;
using EasySave.Services;
using EasySave.ViewModels;
using System.IO;
using System;

namespace EasySave_WPF
{
    public partial class App : Application
    {
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
            var backupManager = new BackupManagerViewModel(
                fileSystemService,
                loggerService,
                stateService,
                persistenceService);

            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainViewModel(
                backupManager,
                languageService,
                loggerService);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            var mainVM = MainWindow?.DataContext as MainViewModel;
            mainVM?.BackupManager.SaveJobs();

            base.OnExit(e);
        }
    }
}

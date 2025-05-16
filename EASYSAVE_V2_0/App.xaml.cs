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
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

            // Initialisation des services
            var loggerService = new LoggerService(Path.Combine(AppContext.BaseDirectory, "Logs"));
            var fileSystemService = new FileSystemService(loggerService);
            var stateService = new StateService(loggerService);
            var languageService = new LanguageService();

            // Initialisation des ViewModels
            var backupManagerViewModel = new BackupManagerViewModel(fileSystemService, loggerService, stateService);
            var mainViewModel = new MainViewModel(backupManagerViewModel, languageService, loggerService);

            // Chargement de la langue des paramètres
            languageService.LoadLanguageFromSettings();

            // Configuration de la fenêtre principale
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
            mainWindow.Show();
        }
    }
}
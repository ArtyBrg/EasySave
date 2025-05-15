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

            var loggerService = new LoggerService();
            var stateService = new StateService(loggerService);
            var fileSystemService = new FileSystemService(loggerService);
            var persistenceService = new PersistenceService(loggerService);
            var backupManager = new BackupManagerViewModel(
                fileSystemService,
                loggerService,
                stateService,
                persistenceService);

            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainViewModel(
                backupManager,
                new LanguageService(),
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
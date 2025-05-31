using System;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace EasySaveClient
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Gestion globale des exceptions non gérées dans le thread UI
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // Gestion globale des exceptions non gérées dans les threads non-UI
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ShowErrorAndShutdown("Unhandled UI exception: " + e.Exception.Message);
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ShowErrorAndShutdown("Unhandled background exception: " + ex.Message);
            }
            else
            {
                ShowErrorAndShutdown("Unhandled background exception.");
            }
        }

        private void ShowErrorAndShutdown(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}

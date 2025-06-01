using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EasySave.Services;
using EasySave.ViewModels;

namespace EasySave_WPF.Views
{
    public partial class ServerStatusView : UserControl
    {
        private RemoteConsoleService? _server;
        private bool _isRunning;

        private TranslationProvider? _translations;

        public ServerStatusView()
        {
            InitializeComponent();
            this.DataContextChanged += ServerStatusView_DataContextChanged;
            Loaded += ServerStatusView_Loaded;
        }

        private void ServerStatusView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                _translations = vm.Translations;
                _translations.PropertyChanged += Translations_PropertyChanged;

                _server = vm.RemoteConsole;
            }
            else
            {
                if (_translations != null)
                    _translations.PropertyChanged -= Translations_PropertyChanged;

                _translations = null;
                _server = null;
            }
            RefreshUI();
        }

        private void Translations_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Le changement de langue signale que toutes les clés ont été mises à jour,
            // on rafraîchit donc l'affichage
            Dispatcher.Invoke(() =>
            {
                RefreshUI();
                UpdateUIState(_isRunning);
            });
        }

        private void ServerStatusView_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            IpTextBlock.Text = $"IP: {GetLocalIPAddress()} | Port: 8080";
            StatusTextBlock.Text = _isRunning ? _translations?["ServerStatusOnline"] ?? "En ligne" : _translations?["ServerStatusOffline"] ?? "Hors ligne";
            StatusTextBlock.Foreground = _isRunning ? Brushes.DarkGreen : Brushes.Red;
            ToggleButton.Content = _isRunning ? _translations?["ButtonStopServer"] ?? "Arrêter le serveur" : _translations?["ButtonStartServer"] ?? "Démarrer le serveur";
        }

        private void UpdateUIState(bool isRunning)
        {
            if (isRunning)
            {
                StatusTextBlock.Text = _translations["ServerStarted"];
                StatusTextBlock.Foreground = Brushes.DarkGreen;

                ToggleButton.Content = _translations["ButtonStopServer"];
                ToggleButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
            else
            {
                StatusTextBlock.Text = _translations["ServerStopped"];
                StatusTextBlock.Foreground = Brushes.DarkRed;

                ToggleButton.Content = _translations["ButtonStartServer"];
                ToggleButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_server == null)
            {
                MessageBox.Show("Le service RemoteConsole n'est pas disponible.");
                return;
            }

            try
            {

                if (_isRunning)
                {
                    StopServer();
                }
                else
                {
                    _server.Start();
                    _isRunning = true;
                }
                RefreshUI();
                UpdateUIState(_isRunning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du démarrage/arrêt du serveur : {ex.Message}");
            }
        }

        private void StopServer()
        {
            if (_server == null) return;
            // Il faut implémenter Stop() dans RemoteConsoleService
            _server.Stop();
            _isRunning = false;
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(i => i.AddressFamily == AddressFamily.InterNetwork);
                return ip?.ToString() ?? "Inconnue";
            }
            catch
            {
                return "Inconnue";
            }
        }
    }
}

using System;
using System.Net.Sockets;
using System.Windows;
using EasySaveClient.ViewModels;

namespace EasySaveClient.Views
{
    public partial class ConnectionWindow : Window
    {
        public ConnectionWindow()
        {
            InitializeComponent();
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            string ip = IpBox.Text;
            if (!int.TryParse(PortBox.Text, out int port))
            {
                MessageBox.Show("Port invalide.");
                return;
            }

            try
            {
                var client = new TcpClient();
                await client.ConnectAsync(ip, port);

                var mainViewModel = new MainViewModel(client);

                var mainWindow = new MainWindow
                {
                    DataContext = mainViewModel
                };

                mainWindow.Show();
                Close(); // ferme la fenêtre de connexion
            }
            catch (Exception ex)
            {
                MessageBox.Show("Échec de connexion : " + ex.Message);
            }
        }
    }
}

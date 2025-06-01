using System.Windows;
using EasySave.ViewModels;

namespace EasySave_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.Resources["AppVM"] = App.AppViewModel;
            InitializeComponent();
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPopup.IsOpen = !SettingsPopup.IsOpen;
        }

    }
}
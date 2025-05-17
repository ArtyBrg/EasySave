using EasySave.ViewModels;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace EasySave_WPF.Views
{
    public partial class DecryptView : UserControl
    {
        private readonly BackupJobViewModel _viewModel;

        public DecryptView(BackupJobViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
        }

        public DecryptView() : this(new BackupJobViewModel())
        {
        }


        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers cryptés (*.crypt)|*.crypt",
                Title = "Sélectionner un fichier chiffré"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            string encryptedPath = FilePathTextBox.Text;

            if (string.IsNullOrWhiteSpace(encryptedPath) || !File.Exists(encryptedPath))
            {
                MessageBox.Show("Veuillez sélectionner un fichier valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string outputPath = encryptedPath.Replace(".crypt", "");

            _viewModel.DecryptFile(encryptedPath, outputPath);

            MessageTextBlock.Text = "Fichier déchiffré avec succès !";
            MessageTextBlock.Visibility = Visibility.Visible;
        }
    }
}

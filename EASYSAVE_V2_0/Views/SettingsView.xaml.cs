using EasySave.Services;
using System.Windows.Controls;

namespace EasySave_WPF.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            var logger = new LoggerService();
            DataContext = new EasySave.ViewModels.SettingsViewModel(logger);
        }
    }
}
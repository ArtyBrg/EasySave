using EasySave.ViewModels;
using System.Configuration;
using System.Windows;

namespace EasySave.Views
{
    public partial class BusinessSoftwarePopup : Window
    {
        public BusinessSoftwarePopup(object viewModel)
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                this.Focus();
            };

            DataContext = viewModel;
        }
    }
}

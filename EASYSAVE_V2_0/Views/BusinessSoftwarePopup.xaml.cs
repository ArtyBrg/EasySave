using System.Windows;

namespace EasySave.Views
{
    public partial class BusinessSoftwarePopup : Window
    {
        public BusinessSoftwarePopup()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                this.Focus();
            };
        }
    }
}

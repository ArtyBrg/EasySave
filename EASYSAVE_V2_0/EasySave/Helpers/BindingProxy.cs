using System.Windows;

namespace EasySave.Helpers
{
    // BindingProxy is used to allow data binding in XAML
    public class BindingProxy : Freezable
    {
        // This is a singleton instance of the BindingProxy
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        // This property is used to bind data to the BindingProxy
        public object Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // This is a dependency property that holds the data
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}

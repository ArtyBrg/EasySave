using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace EasySave_WPF.Converters
{
    public class LanguageButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string currentLanguage = value as string;
            string buttonLanguage = parameter as string;

            if (currentLanguage == buttonLanguage)
            {
                // Bouton actif
                return new SolidColorBrush(Colors.Orange);
            }
            else
            {
                // Bouton inactif
                return new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
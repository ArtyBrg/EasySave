using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EasySave_WPF.Converters
{
    public class LanguageButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string currentLanguage && parameter is string buttonLanguage)
            {
                return currentLanguage == buttonLanguage ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.White);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
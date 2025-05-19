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
            string currentLanguage = value as string;

            if (parameter is string paramStr && !string.IsNullOrEmpty(currentLanguage))
            {
                // Exemple paramStr : "FR|Settings" ou "EN|App"
                var parts = paramStr.Split('|');
                if (parts.Length == 2)
                {
                    string buttonLanguage = parts[0];
                    string context = parts[1];

                    // Comparer le bouton à la langue actuelle
                    if (string.Equals(currentLanguage, buttonLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        return context == "Settings" ? Brushes.Orange : Brushes.Orange;
                    }
                }
            }

            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EasySave_WPF.Converters
{
    // Edit of the LanguageButtonColorConverter to use a different color for Settings and App buttons
    public class LanguageButtonColorConverter : IValueConverter
    {
        // Convert method to change the color of the button based on the current language
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string currentLanguage = value as string;

            // Verify if the parameter is a string and contains the language and context
            if (parameter is string paramStr && !string.IsNullOrEmpty(currentLanguage))
            {
                // Example paramStr : "FR|Settings" or "EN|App"
                var parts = paramStr.Split('|');
                if (parts.Length == 2)
                {
                    string buttonLanguage = parts[0];
                    string context = parts[1];

                    // Compare the current language with the button language
                    if (string.Equals(currentLanguage, buttonLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        return context == "Settings" ? Brushes.Orange : Brushes.Orange;
                    }
                }
            }

            return Brushes.White;
        }

        // ConvertBack method is not implemented as it's not needed for this converter
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EasySave_WPF.Converters
{
    // Makes a boolean value visible or collapsed
    public class BooleanToVisibilityConverter : IValueConverter
    {
        // Converts a boolean value to a Visibility value
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a boolean and convert it to Visibility
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        // Converts a Visibility value back to a boolean value
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a Visibility and convert it to boolean
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
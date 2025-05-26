using System;
using System.Globalization;
using System.Windows.Data;

namespace EasySave_WPF.Converters
{
    // Converts a string value to a boolean for RadioButton checked state
    public class RadioButtonCheckedConverter : IValueConverter
    {
        // Converts a string value to a boolean for RadioButton checked state
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        // Converts a boolean value back to a string for RadioButton checked state
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? parameter?.ToString() : Binding.DoNothing;
        }
    }
}

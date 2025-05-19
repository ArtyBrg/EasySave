using System;
using System.Globalization;
using System.Windows.Data;

namespace EasySave_WPF.Converters
{
    // Converts a boolean value to a string representing the job status
    public class JobStatusConverter : IValueConverter
    {
        // Converts a boolean value to a string representing the job status
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the value is a boolean and convert it to a string
            if (value is bool isRunning)
            {
                return isRunning ? "Running" : "Idle";
            }
            return "Unknown";
        }

        // Converts a string value back to a boolean value
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
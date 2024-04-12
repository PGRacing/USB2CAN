using System;
using System.Globalization;
using System.Windows.Data;

namespace CanApp.Converters
{
    public class IntToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue.ToString("X"); // Format heksadecymalny
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Opcjonalnie, jeśli potrzebujesz konwersji w drugą stronę
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CanApp.Converters
{
    public class ByteVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int maxBytes && parameter is string byteIndexString && int.TryParse(byteIndexString, out int byteIndex))
            {
                return byteIndex < maxBytes ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

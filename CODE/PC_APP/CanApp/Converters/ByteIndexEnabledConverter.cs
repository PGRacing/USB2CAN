using System;
using System.Globalization;
using System.Windows.Data;

namespace CanApp.Converters
{
    public class ByteIndexEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int maxBytes = (int)value;
            int byteIndex = System.Convert.ToInt32(parameter);
            return byteIndex < maxBytes;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Conversion back is not supported.");
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;


namespace CanApp.Converters
{
    public class ByteArrayToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var byteArray = value as byte[];
            if (byteArray == null) return string.Empty;

            // Przykładowo konwertujemy tablicę bajtów na ciąg heksadecymalny rozdzielony spacjami
            return BitConverter.ToString(byteArray).Replace("-", " ");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

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
            if (byteArray == null)
                return string.Empty;

            // Inicjalizacja tablicy znaków, która będzie przechowywać wynik konwersji
            char[] chars = new char[byteArray.Length * 3];
            for (int i = 0; i < byteArray.Length; i++)
            {
                // Przekształcenie każdego bajtu na dwie cyfry szesnastkowe plus spacja
                int b = byteArray[i] >> 4;
                chars[i * 3] = (char)(55 + b + (((b - 10) >> 31) & -7));
                b = byteArray[i] & 0xF;
                chars[i * 3 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
                chars[i * 3 + 2] = ' ';
            }

            // Zwrócenie wynikowego stringa (bez ostatniej spacji)
            return new string(chars, 0, chars.Length - 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Konwersja wsteczna nie jest potrzebna, więc nie jest implementowana
            throw new NotImplementedException();
        }
    }
}

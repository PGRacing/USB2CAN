using System;
using System.Collections.Generic;
using System.Linq;

namespace CanApp.Models
{
    public class CanFrame
    {
        public byte StartMarker { get; set; }
        public int ID { get; set; }
        public int DLC { get; set; }
        public List<byte> Data { get; set; } = new List<byte>();
        public byte EndMarker { get; set; }

        // Timestamp przechowuje informacje o czasie, w którym ramka została utworzona lub otrzymana.
        public DateTime Timestamp { get; set; }

        // DataDisplay służy do reprezentowania danych w ramce jako ciągu znaków w formacie heksadecymalnym.
        public string DataDisplay => string.Join(" ", Data.Select(b => b.ToString("X2")));

        // TimestampString zwraca Timestamp w formacie ciągu znaków, zawierającym datę, czas oraz milisekundy.
        public string TimestampString => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}

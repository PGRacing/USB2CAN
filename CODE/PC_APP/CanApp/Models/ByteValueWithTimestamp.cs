using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanApp.Models
{
    public class ByteValueWithTimestamp
    {
        public string ByteValue { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimestampString => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}

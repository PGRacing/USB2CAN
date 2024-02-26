using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanApp.ViewModels
{
    public class MyDataModel
    {
        public int ID { get; set; }
        public int DLC { get; set; }
        public byte[] Bytes { get; set; }
        public string HexID
        {
            get { return ID.ToString("X"); } // Konwertuje wartość ID na heksadecymalną
        }
    }
}

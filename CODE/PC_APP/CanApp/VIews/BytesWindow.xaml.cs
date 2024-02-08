using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CanApp;
using CanApp.ViewModels;

namespace CanApp.VIews
{
    /// <summary>
    /// Logika interakcji dla klasy BytesWindow.xaml
    /// </summary>
    public partial class BytesWindow : Window
    {
        public BytesWindow(List<byte> byteValues)
        {
            InitializeComponent();
            // Ustaw źródło danych dla kontrolki wyświetlającej bajty, na przykład ListBox
            this.ListBoxByteValues.ItemsSource = byteValues.Select(b => b.ToString("X2"));
        }
    }

}


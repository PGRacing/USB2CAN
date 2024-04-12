using CanApp.Models;
using CanApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace CanApp.Views
{
    /// <summary>
    /// Logika interakcji dla klasy DetailsWindow.xaml
    /// </summary>
    public partial class DetailsWindow : Window
    {
        private DetailsViewModel _viewModel;

        public DetailsWindow(Dictionary<int, ObservableCollection<CanFrame>> framesById)
        {
            InitializeComponent();
            _viewModel = new DetailsViewModel { FramesById = framesById };
            this.DataContext = _viewModel;
        }
        private void OnByteButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Debug.WriteLine("Przycisk kliknięty");
                Debug.WriteLine($"Typ sendera: {sender.GetType().ToString()}");
                Debug.WriteLine($"Wartość Tag: {button.Tag?.ToString() ?? "null"}");

                if (int.TryParse(button.Tag?.ToString(), out int byteIndex)) // Próba konwersji Tag na int
                {
                    Debug.WriteLine($"Wszedłeś do ifa z byteIndex: {byteIndex}");
                    var byteValuesWithTimestamps = _viewModel.GetByteValuesForIndex(byteIndex);
                    var byteValuesWindow = new ByteValuesWindow(byteValuesWithTimestamps); // Uwzględnij zmianę konstruktora
                    byteValuesWindow.Show();
                }
                else
                {
                    Debug.WriteLine("Tag nie jest konwertowalny do int");
                }
            }
            else
            {
                Debug.WriteLine("Sender nie jest przyciskiem");
            }
        }



    }
}

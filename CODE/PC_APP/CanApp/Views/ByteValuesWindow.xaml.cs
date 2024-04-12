using CanApp.Models;
using CanApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Logika interakcji dla klasy ByteValuesWindow.xaml
    /// </summary>
    public partial class ByteValuesWindow : Window
    {
        public ByteValuesWindow(IEnumerable<ByteValueWithTimestamp> byteValuesWithTimestamps)
        {
            InitializeComponent();
            DataContext = new ByteValuesViewModel { ByteValuesWithTimestamps = new ObservableCollection<ByteValueWithTimestamp>(byteValuesWithTimestamps) };
        }

        private void ShowChartWindow(object sender, RoutedEventArgs e)
        {
            if (DataContext is ByteValuesViewModel viewModel)
            {
                // Przygotuj dane do wykresu
                var data = viewModel.ByteValuesWithTimestamps;

                var chartWindowViewModel = new ChartWindowViewModel(data.ToList());
                var chartWindow = new ChartWindow(chartWindowViewModel);
                chartWindow.Show();
            }
        }


    }
}

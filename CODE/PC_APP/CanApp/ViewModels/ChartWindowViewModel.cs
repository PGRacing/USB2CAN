using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanApp.Models;
using LiveCharts.Defaults;
using System.Diagnostics;

namespace CanApp.ViewModels
{
    public class ChartWindowViewModel : INotifyPropertyChanged
    {
        private SeriesCollection _seriesCollection;
        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            set
            {
                _seriesCollection = value;
                OnPropertyChanged(nameof(SeriesCollection));
            }
        }

        // Konstruktor przyjmujący dane do wykresu
        public Func<double, string> DateTimeFormatter { get; set; }

        public ChartWindowViewModel(IEnumerable<ByteValueWithTimestamp> data)
        {
            // Ta funkcja konwertuje wartości tick na czytelny format daty
            DateTimeFormatter = value => new DateTime((long)value).ToString("G");

            SeriesCollection = new SeriesCollection
                {
                   new LineSeries
                    {
                        Title = "Bajt",
                        Values = new ChartValues<DateTimePoint>(
                            data.Select(d =>
                            {
                                double byteValue = 0;
                                if (!string.IsNullOrEmpty(d.ByteValue))
                                {
                                    byteValue = Convert.ToInt32(d.ByteValue, 16);
                                }
                                // Bezpośrednio używaj DateTime z Timestamp, nie konwertuj na OLE Automation Date
                                return new DateTimePoint(d.Timestamp, byteValue); // Tu następuje poprawka
                            })
                        )
                    }
                };
            OnPropertyChanged(nameof(SeriesCollection));
            OnPropertyChanged(nameof(DateTimeFormatter));
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}

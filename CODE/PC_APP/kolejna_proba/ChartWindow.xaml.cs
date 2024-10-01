using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;

namespace kolejna_proba
{
    public partial class ChartWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ChartValues<double> values;
        private List<string> times;

        public ChartWindow(List<CanFrame> frames, int selectedId)
        {
            InitializeComponent();

            values = new ChartValues<double>();
            times = new List<string>();

            // Przygotuj dane do wykresu
            PrepareChart(frames, selectedId);
        }

        private void PrepareChart(List<CanFrame> frames, int selectedId)
        {
            foreach (var frame in frames)
            {
                if (frame.Data.Count > 0)
                {
                    // Zakładamy, że pierwszy bajt danych jest wartością do wykresu
                    double dataValue = frame.Data[0];

                    values.Add(dataValue);
                    times.Add(frame.Timestamp.ToString("HH:mm:ss.fff"));
                }
            }

            cartesianChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = $"ID {selectedId}",
                    Values = values,
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 5
                }
            };

            cartesianChart.AxisX.Add(new Axis
            {
                Title = "Czas",
                Labels = times,
                LabelsRotation = 45
            });

            cartesianChart.AxisY.Add(new Axis
            {
                Title = "Wartość danych",
            });

            cartesianChart.Zoom = ZoomingOptions.X;
        }

        public void UpdateChart(CanFrame frame)
        {
            if (frame.Data.Count > 0)
            {
                double dataValue = frame.Data[0];

                values.Add(dataValue);
                times.Add(frame.Timestamp.ToString("HH:mm:ss.fff"));

                // Aktualizacja osi X
                cartesianChart.AxisX[0].Labels = times;

                // Powiadomienie o zmianie
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Values"));
            }
        }
    }
}

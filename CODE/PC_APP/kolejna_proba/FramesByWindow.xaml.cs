using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace kolejna_proba
{
    public partial class FramesByIdWindow : Window
    {
        private List<CanFrame> allFrames; // Lista wszystkich ramek
        private int maxFrames = 200; // Maksymalna liczba ramek do wyświetlenia

        private ChartWindow chartWindow; // Okno wykresu

        public FramesByIdWindow(List<CanFrame> frames)
        {
            InitializeComponent();

            allFrames = frames;

            // Pobierz unikalne ID ramek
            var frameIds = allFrames.Select(f => f.ID).Distinct().OrderBy(id => id).ToList();

            // Wypełnij ComboBox
            comboBoxFrameIds.ItemsSource = frameIds;
            if (frameIds.Count > 0)
                comboBoxFrameIds.SelectedIndex = 0;
        }

        private void comboBoxFrameIds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDataGrid();
        }

        private void UpdateDataGrid()
        {
            if (comboBoxFrameIds.SelectedItem != null)
            {
                int selectedId = (int)comboBoxFrameIds.SelectedItem;
                // Filtruj ramki z wybranym ID
                var framesById = allFrames.Where(f => f.ID == selectedId).OrderByDescending(f => f.SequenceId).Take(maxFrames).ToList();

                dataGridFramesById.ItemsSource = framesById;
            }
        }

        public void UpdateFrames(List<CanFrame> newFrames)
        {
            // Dodaj nowe ramki do listy
            allFrames.AddRange(newFrames);

            // Jeśli jakieś ID jest nowe, dodaj je do ComboBox
            var newIds = newFrames.Select(f => f.ID).Distinct().Except(comboBoxFrameIds.Items.Cast<int>()).ToList();
            foreach (var id in newIds)
            {
                comboBoxFrameIds.Items.Add(id);
            }

            // Odśwież DataGrid
            UpdateDataGrid();

            // Przekaż nowe ramki do ChartWindow
            if (chartWindow != null && chartWindow.IsVisible)
            {
                foreach (var frame in newFrames)
                {
                    if (frame.ID == (int)comboBoxFrameIds.SelectedItem)
                    {
                        chartWindow.UpdateChart(frame);
                    }
                }
            }
        }

        private void ShowChartButton_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxFrameIds.SelectedItem != null)
            {
                int selectedId = (int)comboBoxFrameIds.SelectedItem;

                if (chartWindow == null || !chartWindow.IsVisible)
                {
                    // Filtruj ramki z wybranym ID
                    var framesById = allFrames.Where(f => f.ID == selectedId).OrderBy(f => f.SequenceId).ToList();

                    chartWindow = new ChartWindow(framesById, selectedId);
                    chartWindow.Show();
                }
                else
                {
                    chartWindow.Activate();
                }
            }
            else
            {
                MessageBox.Show("Proszę wybrać ID ramki.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

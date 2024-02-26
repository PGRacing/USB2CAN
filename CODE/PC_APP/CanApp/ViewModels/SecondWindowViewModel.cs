using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CanApp.VIews;

namespace CanApp.ViewModels
{
    public class SecondWindowViewModel : INotifyPropertyChanged
    {
        public MyViewModel MainViewModel { get; private set; }
        public ICommand OpenBytesWindowCommand { get; private set; }

        private string _selectedHexId;

        private int _maxBytes;
        private ObservableCollection<MyDataModel> _filteredDataGridCollection;
        public string SelectedHexId
        {
            get { return _selectedHexId; }
            set
            {
                if (_selectedHexId != value)
                {
                    _selectedHexId = value;
                    OnPropertyChanged(nameof(SelectedHexId));
                    FilterDataGridCollection();
                }
            }
        }

        public int MaxBytes
        {
            get { return _maxBytes; }
            set
            {
                if (_maxBytes != value)
                {
                    _maxBytes = value;
                    OnPropertyChanged(nameof(MaxBytes));
                }
            }
        }

        public SecondWindowViewModel(MyViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
            FilteredDataGridCollection = new ObservableCollection<MyDataModel>();
            OpenBytesWindowCommand = new RelayCommand(OpenBytesWindow);
        }

        public ObservableCollection<MyDataModel> FilteredDataGridCollection
        {
            get { return _filteredDataGridCollection; }
            set
            {
                if (_filteredDataGridCollection != value)
                {
                    _filteredDataGridCollection = value;
                    OnPropertyChanged(nameof(FilteredDataGridCollection));
                }
            }
        }

     

        private void FilterDataGridCollection()
        {
            if (!string.IsNullOrWhiteSpace(SelectedHexId) && MainViewModel.FramesById.ContainsKey(SelectedHexId))
            {
                var filteredData = MainViewModel.FramesById[SelectedHexId];

                // Zaktualizuj przefiltrowaną kolekcję
                FilteredDataGridCollection.Clear();
                if (filteredData != null)
                {
                    foreach (var item in filteredData)
                    {
                        if (item != null)
                        {
                            FilteredDataGridCollection.Add(item);
                        }
                    }
                }

                // Ustaw MaxBytes na maksymalną długość bajtów wśród wszystkich ramek dla wybranego HexID
                MaxBytes = filteredData.Max(frame => frame?.Bytes.Length ?? 0);

            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenBytesWindow(object parameter)
        {
            if (parameter == null)
            {
                // Logika obsługi błędów lub wczesny return, jeśli parametr jest null
                return;
            }

            // Spróbuj przekonwertować parametr na int
            if (int.TryParse(parameter.ToString(), out int byteIndex))
            {
                if (!string.IsNullOrWhiteSpace(SelectedHexId))
                {
                    // Sprawdź, czy słownik zawiera klucz SelectedHexId
                    if (MainViewModel.FramesById.TryGetValue(SelectedHexId, out var framesWithSelectedId))
                    {
                        // Teraz masz listę ramek z danym HexID
                        var byteValues = framesWithSelectedId
                            .Select(frame => frame.Bytes.ElementAtOrDefault(byteIndex))
                            .ToList();

                        // Teraz możesz otworzyć okno z wartościami bajtów dla tego konkretnego indeksu bajtu
                        BytesWindow bytesWindow = new BytesWindow(byteValues);
                        bytesWindow.Show();
                    }
                }
            }
            else
            {
                // Logika obsługi błędów, jeśli parametr nie mógł być przekonwertowany na int
            }
        }


    }
}

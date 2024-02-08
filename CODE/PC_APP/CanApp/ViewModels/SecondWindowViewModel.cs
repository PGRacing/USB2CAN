using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using CanApp.VIews;

namespace CanApp.ViewModels
{
    public class SecondWindowViewModel : INotifyPropertyChanged
    {
        public MyViewModel MainViewModel { get; private set; }
        public ICommand OpenBytesWindowCommand { get; private set; }

        private int _selectedId;
        private int _maxBytes;
        private ObservableCollection<MyDataModel> _filteredDataGridCollection;
        public int SelectedId
        {
            get { return _selectedId; }
            set
            {
                if (_selectedId != value)
                {
                    _selectedId = value;
                    OnPropertyChanged(nameof(SelectedId));
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

        public SecondWindowViewModel(MyViewModel mainViewModel)
        {
            MainViewModel = mainViewModel;
            FilteredDataGridCollection = new ObservableCollection<MyDataModel>();
            OpenBytesWindowCommand = new RelayCommand(OpenBytesWindow);
        }

        private void FilterDataGridCollection()
        {
            if (MainViewModel != null && MainViewModel.DataGridCollection1 != null)
            {
                var filteredData = MainViewModel.DataGridCollection1.Where(item => item.ID == SelectedId).ToList();

                // Zaktualizuj przefiltrowaną kolekcję
                FilteredDataGridCollection.Clear();
                foreach (var item in filteredData)
                {
                    FilteredDataGridCollection.Add(item);
                }

                // Aktualizuj MaxBytes na podstawie długości danych bajtowych pierwszego elementu z przefiltrowanych danych
                MaxBytes = filteredData.FirstOrDefault()?.Bytes?.Length ?? 0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenBytesWindow(object parameter)
        {
            int byteIndex = Convert.ToInt32(parameter);
            var selectedFrame = MainViewModel.DataGridCollection1.FirstOrDefault(x => x.ID == SelectedId);
            if (selectedFrame == null || byteIndex >= selectedFrame.Bytes.Length)
            {
                // Bajt jest poza zakresem, więc nic nie rób.
                return;
            }

            var byteValues = MainViewModel.DataGridCollection1
                .Where(x => x.ID == SelectedId)
                .Select(frame => frame.Bytes.ElementAtOrDefault(byteIndex))
                .ToList();

            BytesWindow bytesWindow = new BytesWindow(byteValues);
            bytesWindow.Show();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace WpfApp1.ViewModels
{
    public class SecondWindowViewModel : INotifyPropertyChanged
    {
        public MyViewModel MainViewModel { get; private set; }

        private int _selectedId;
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

        private ObservableCollection<MyDataModel> _filteredDataGridCollection;
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
        }

        private void FilterDataGridCollection()
        {
            if (MainViewModel != null && MainViewModel.DataGridCollection != null)
            {
                // Pobierz przefiltrowane dane z DataGridCollection w oparciu o wybrane ID
                var filteredData = MainViewModel.DataGridCollection.Where(item => item.ID == SelectedId);

                // Zaktualizuj przefiltrowaną kolekcję
                FilteredDataGridCollection.Clear();
                foreach (var item in filteredData)
                {
                    FilteredDataGridCollection.Add(item);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

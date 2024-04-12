using CanApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CanApp.ViewModels
{
    public class ByteValuesViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<ByteValueWithTimestamp> _byteValuesWithTimestamps;
        public ObservableCollection<ByteValueWithTimestamp> ByteValuesWithTimestamps
        {
            get => _byteValuesWithTimestamps;
            set
            {
                _byteValuesWithTimestamps = value;
                OnPropertyChanged(nameof(ByteValuesWithTimestamps));
            }
        }

        public ByteValuesViewModel()
        {
            ByteValuesWithTimestamps = new ObservableCollection<ByteValueWithTimestamp>();
        }

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

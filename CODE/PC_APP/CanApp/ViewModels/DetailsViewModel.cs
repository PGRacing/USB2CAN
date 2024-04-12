using CanApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanApp.ViewModels
{
    public class DetailsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Dictionary<int, ObservableCollection<CanFrame>> _framesById;
        public Dictionary<int, ObservableCollection<CanFrame>> FramesById
        {
            get => _framesById;
            set
            {
                _framesById = value;
                OnPropertyChanged(nameof(FramesById));
                AvailableIds = new ObservableCollection<int>(_framesById.Keys);
            }
        }

        private ObservableCollection<int> _availableIds;
        public ObservableCollection<int> AvailableIds
        {
            get => _availableIds;
            set
            {
                _availableIds = value;
                OnPropertyChanged(nameof(AvailableIds));
            }
        }

        private int? _selectedId;
        public int? SelectedId
        {
            get => _selectedId;
            set
            {
                if (_selectedId != value)
                {
                    _selectedId = value;
                    SelectedFrames = _selectedId.HasValue ? FramesById[_selectedId.Value] : null;
                    OnPropertyChanged(nameof(SelectedId));
                    OnPropertyChanged(nameof(SelectedFrames));
                }
            }
        }

        private ObservableCollection<CanFrame> _selectedFrames;
        public ObservableCollection<CanFrame> SelectedFrames
        {
            get => _selectedFrames;
            set
            {
                if (_selectedFrames != value)
                {
                    _selectedFrames = value;
                    OnPropertyChanged(nameof(SelectedFrames));
                    UpdateMaxDataBytes(); // Aktualizuj MaxDataBytes za każdym razem, gdy zmienia się SelectedFrames
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private int _selectedByteIndex = -1; // -1 oznacza, że żaden bajt nie jest wybrany

        public void SelectByte(int byteIndex)
        {
            _selectedByteIndex = byteIndex;
            OnPropertyChanged(nameof(ByteValues)); // Powiadamia o zmianie filtru bajtów
        }

        public IEnumerable<string> ByteValues
        {
            get
            {
                if (_selectedByteIndex >= 0)
                {
                    // Załóżmy, że CanFrame ma właściwość 'Bytes' typu byte[]
                    return SelectedFrames.Select(frame => frame.Data.Count > _selectedByteIndex ? frame.Data[_selectedByteIndex].ToString("X2") : "N/A");
                }
                return Enumerable.Empty<string>();
            }
        }

        private int _maxDataBytes = 0;
        public int MaxDataBytes
        {
            get => _maxDataBytes;
            set
            {
                _maxDataBytes = value;
                OnPropertyChanged(nameof(MaxDataBytes));
            }
        }

        public void UpdateMaxDataBytes()
        {
            if (SelectedFrames != null && SelectedFrames.Any())
            {
                MaxDataBytes = SelectedFrames.Max(frame => frame.Data?.Count ?? 0);
            }
            else
            {
                MaxDataBytes = 0;
            }
        }

        public IEnumerable<ByteValueWithTimestamp> GetByteValuesForIndex(int byteIndex)
        {
            return SelectedFrames.Select(frame =>
                new ByteValueWithTimestamp
                {
                    ByteValue = frame.Data.Count > byteIndex ? frame.Data[byteIndex].ToString("X2") : "N/A",
                    Timestamp = frame.Timestamp
                }).ToList();
        }


    }
}

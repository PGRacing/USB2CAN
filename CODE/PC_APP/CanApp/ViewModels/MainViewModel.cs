using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading;
using System.Windows.Input;
using CanApp.Commands;
using CanApp.Models;
using System.Windows;
using System.Windows.Threading;
using CanApp.Views;
using System.Diagnostics;

namespace CanApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private SerialPort serialPort;
        private Thread processingThread;
        private object lockObject = new object();
        private Queue<byte> buffer = new Queue<byte>();
        public ObservableCollection<CanFrame> Frames { get; } = new ObservableCollection<CanFrame>();
        public ObservableCollection<string> AvailablePorts { get; } = new ObservableCollection<string>(SerialPort.GetPortNames());
        private bool keep_procesing;
        public Dictionary<int, ObservableCollection<CanFrame>> FramesById { get; } = new Dictionary<int, ObservableCollection<CanFrame>>();

        public bool IsStartEnabled
        {
            get { return !serialPort.IsOpen && !string.IsNullOrEmpty(SelectedPort); }
        }

        public bool IsStopEnabled
        {
            get { return serialPort.IsOpen; }
        }
        private string _selectedPort;
        public string SelectedPort
        {
            get => _selectedPort;
            set
            {
                _selectedPort = value;
                OnPropertyChanged(nameof(SelectedPort));
                OnPropertyChanged(nameof(IsStartEnabled));
            }
        }

        private int _maxFrames = 1000; // Domyślna wartość

        public int MaxFrames
        {
            get => _maxFrames;
            set
            {
                if (value != _maxFrames)
                {
                    _maxFrames = value;
                    OnPropertyChanged(nameof(MaxFrames));
                }
            }
        }

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ClearFramesCommand { get; }
        public ICommand ShowDetailsCommand { get; }
        private Stopwatch stopwatch;
        private DateTime startTime;

        public MainViewModel()
        {
            serialPort = new SerialPort { BaudRate = 115200, Parity = Parity.None, DataBits = 8, StopBits = StopBits.One };
            serialPort.DataReceived += SerialPort_DataReceived;

            StartCommand = new RelayCommand(StartAction);
            StopCommand = new RelayCommand(StopAction);
            ClearFramesCommand = new RelayCommand(ClearFramesAction);
            ShowDetailsCommand = new RelayCommand(ShowDetailsAction);
            stopwatch = Stopwatch.StartNew();
            startTime = DateTime.UtcNow;
        }

        private void StartAction(object parameter)
        {
            if (!serialPort.IsOpen && !string.IsNullOrEmpty(SelectedPort))
            {
                buffer.Clear();
                serialPort.PortName = SelectedPort;
                keep_procesing = true;
                serialPort.Open();

                processingThread = new Thread(ProcessData);
                processingThread.Start();
                OnPropertyChanged(nameof(IsStartEnabled));
                OnPropertyChanged(nameof(IsStopEnabled));
            }
        }

        private void StopAction(object parameter)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close(); 

                keep_procesing = false;

                lock (buffer)
                {
                    buffer.Clear();
                }
                OnPropertyChanged(nameof(IsStartEnabled));
                OnPropertyChanged(nameof(IsStopEnabled));
            }
        }

        private void ClearFramesAction(object parameter)
        {
            Frames.Clear();
        }


        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = serialPort.BytesToRead;
            byte[] receivedBytes = new byte[bytesToRead];
            serialPort.Read(receivedBytes, 0, bytesToRead);

            lock (buffer)
            {
                foreach (byte b in receivedBytes)
                {
                    buffer.Enqueue(b);
                }
            }
        }

        private void ProcessData()
        {
            while (keep_procesing)
            {
                CanFrame frame = null;

                // Get the current time as a timestamp for the potential frame
                DateTime timestamp = startTime.Add(stopwatch.Elapsed);

                // Attempt to parse a frame from the buffer, passing in the timestamp
                if (TryParseFrame(timestamp, out frame))
                {
                    AddFrameToCollection(frame);
                    UpdateFramesById(frame);
                }
                
            }
        }

        private void AddFrameToCollection(CanFrame frame)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Frames.Count >= MaxFrames)
                {
                    Frames.RemoveAt(0); // Usuwa najstarszą ramkę, aby utrzymać ograniczenie do MaxFrames
                }
                Frames.Add(frame); // Dodaje nową ramkę
            });
        }

        private void UpdateFramesById(CanFrame frame)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {

                if (!FramesById.ContainsKey(frame.ID))
                {
                    FramesById[frame.ID] = new ObservableCollection<CanFrame>();
                }

                var framesList = FramesById[frame.ID];
                framesList.Add(frame);
                while (framesList.Count > 100) // Ogranicz każdą listę do ostatnich 100 ramek
                {
                    framesList.RemoveAt(0);
                }
                
            });
        }

        private void ShowDetailsAction(object parameter)
        {
            var detailsWindow = new DetailsWindow(FramesById);
            detailsWindow.Show();
        }


        public bool TryParseFrame(DateTime timestamp, out CanFrame frame)
        {
            lock (buffer)
            {
                frame = null;
                while (buffer.Count >= 4) // Zakładamy minimalną długość ramki: start marker, 2 bajty ID, DLC, i przynajmniej 1 bajt danych
                {
                    if (buffer.Peek() == 0xAA) // Start of frame detected
                    {
                        var tempFrame = new CanFrame { StartMarker = buffer.Dequeue() };

                        // Pobranie DLC i ID ramki
                        byte dlcAndMore = buffer.Dequeue(); // Zakładamy, że pierwszy bajt po markerze to DLC
                        tempFrame.DLC = dlcAndMore & 0x0F; // Zakładamy, że 4 młodsze bity to DLC
                        int idHigh = buffer.Dequeue(); // High byte of the ID
                        int idLow = buffer.Dequeue();  // Low byte of the ID
                        tempFrame.ID = (idHigh << 8) + idLow; // Składamy ID z dwóch bajtów

                        // Sprawdzenie, czy mamy wystarczająco danych w buforze na podstawie DLC
                        if (buffer.Count >= tempFrame.DLC + 1) // +1 dla end markera
                        {
                            for (int i = 0; i < tempFrame.DLC; i++)
                            {
                                tempFrame.Data.Add(buffer.Dequeue()); // Dodajemy dane do ramki
                            }

                            if (buffer.Peek() == 0x55) // Sprawdzamy end marker
                            {
                                tempFrame.EndMarker = buffer.Dequeue();
                                tempFrame.Timestamp = timestamp; // Przypisujemy timestamp
                                frame = tempFrame;
                                return true; // Zwracamy true, gdy ramka zostanie poprawnie zbudowana
                            }
                            else
                            {
                                // Jeśli end marker się nie zgadza, można zalogować błąd lub odrzucić ramkę
                            }
                        }
                        else
                        {
                            // Jeśli nie ma wystarczająco danych, przerywamy pętlę i czekamy na więcej danych
                            break;
                        }
                    }
                    else
                    {
                        buffer.Dequeue(); // Odrzucamy bajty, które nie są start markerem
                    }
                }
            }
            return false; // Zwracamy false, jeśli nie udało się zbudować ramki
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

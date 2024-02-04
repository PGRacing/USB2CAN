using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using WpfApp1.VIews;


namespace WpfApp1.ViewModels
{
    public class MyViewModel
    {
        static SerialPort _serialPort;
        public ObservableCollection<MyDataModel> DataGridCollection { get; set; }
        public ICommand OpenPortCommand { get; private set; }
        public ICommand ClosePortCommand { get; private set; }
        public ICommand ExportToCsvCommand { get; private set; }
        public ICommand ClearDataGridCommand { get; private set; }
        public ICommand OpenNewWindowCommand { get; private set; }
        public ObservableCollection<int> AvailableFrameIds { get; private set; }

        private Dictionary<int, DateTime> lastReceivedTimestamps = new Dictionary<int, DateTime>();

        public MyViewModel()
        {
            DataGridCollection = new ObservableCollection<MyDataModel>();
            // Konfiguracja portu szeregowego (przykład, dostosuj do swoich potrzeb)

            OpenPortCommand = new RelayCommand(OpenPort);
            ClosePortCommand = new RelayCommand(ClosePort, CanClosePort);
            ExportToCsvCommand = new RelayCommand(ExportToCsv);
            ClearDataGridCommand = new RelayCommand(ClearDataGrid);
            _serialPort = new SerialPort("COM4", 115200);
            _serialPort.DataReceived += SerialPort_DataReceived;
            OpenNewWindowCommand = new RelayCommand(OpenNewWindow);
            AvailableFrameIds = new ObservableCollection<int>();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            List<byte> buffer = new List<byte>();
            bool packetStartDetected = false;
            int expectedLength = 0;
            int bytesRead = 0;

            while (_serialPort.BytesToRead > 0)
            {
                var readByte = (byte)_serialPort.ReadByte();

                if (!packetStartDetected)
                {
                    if (readByte == 0xAA)
                    {
                        packetStartDetected = true;
                        buffer.Clear(); // Wyczyść bufor, aby rozpocząć nową ramkę
                        bytesRead = 1;
                        buffer.Add(readByte);
                    }
                }
                else
                {
                    buffer.Add(readByte);
                    bytesRead++;

                    if (bytesRead == 2)
                    {
                        expectedLength = readByte & 0x0F;
                        expectedLength += 5;
                    }

                    if (bytesRead == expectedLength)
                    {
                        var canData = ConvertBytesToCanFrame(buffer.ToArray());
                        if (canData != null)
                        {
                            double frequency = UpdateFrameFrequency(canData.ID);
                            canData.Frequency = frequency;

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                DataGridCollection.Add(canData);
                                if (!AvailableFrameIds.Contains(canData.ID))
                                {
                                    AvailableFrameIds.Add(canData.ID);
                                }
                            });
                        }
                        else
                        {
                            Debug.WriteLine("Odebrana ramka jest nieprawidłowa lub ma niewłaściwy format.");
                        }

                        buffer.Clear();
                        packetStartDetected = false;
                        bytesRead = 0;
                        expectedLength = 0;
                    }
                }
            }
        }

        private MyDataModel ConvertBytesToCanFrame(byte[] bytes)
        {
            if (bytes.Length < 5 || bytes[0] != 0xAA || bytes[bytes.Length - 1] != 0x55)
            {
                return null;
            }

            // Usuń nagłówek i kod zakończenia
            byte[] frameBytes = bytes.Skip(1).Take(bytes.Length - 2).ToArray();

            int typeByte = frameBytes[0];
            int dlc = typeByte & 0x0F; // Długość danych zapisana w bitach 0-3

            if (frameBytes.Length != 3 + dlc) // 1 bajt typu, 2 bajty ID, dlc bajtów danych
            {
                Debug.WriteLine("Bytes  2:");
                foreach (byte b in bytes)
                {
                    Debug.Write(b.ToString("X2") + " ");
                }
                Debug.WriteLine("\n");
                return null;
            }

            // Wyodrębnij ID ramki (2 bajty)
            int id = (frameBytes[1] << 8) + frameBytes[2];

            string dataBytes = BitConverter.ToString(frameBytes, 3, dlc).Replace("-", " ");

            // Utwórz i zwróć instancję MyDataModel
            return new MyDataModel
            {
                ID = id,
                DLC = dlc,
                Bytes = dataBytes
            };
        }

        private double UpdateFrameFrequency(int frameId)
        {
            DateTime now = DateTime.Now;
            double frequency = 0;

            if (lastReceivedTimestamps.TryGetValue(frameId, out DateTime lastReceivedTime))
            {
                TimeSpan timeDifference = now - lastReceivedTime;
                frequency = 1 / timeDifference.TotalSeconds; // Oblicz częstotliwość
                frequency = Math.Round(frequency, 0);
                lastReceivedTimestamps[frameId] = now; // Aktualizuj czas ostatniego odbioru
            }
            else
            {
                lastReceivedTimestamps.Add(frameId, now); // Pierwszy odbiór ramki z tym ID
            }

            return frequency;
        }

        private void OpenPort(object param) 
        {
            if (_serialPort != null && !_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Open();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void ClosePort(object param) 
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            }
        }

       
        private bool CanClosePort(object param)
        {
            return _serialPort != null && _serialPort.IsOpen;
        }

        private void ExportToCsv(object param)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                DefaultExt = "csv",
                AddExtension = true
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                using (var sw = new StreamWriter(saveFileDialog.FileName, false))
                {
                    // Write headers
                    var headers = DataGridCollection.FirstOrDefault()?.GetType().GetProperties().Select(prop => prop.Name);
                    var headerLine = string.Join(",", headers);
                    sw.WriteLine(headerLine);

                    // Write data
                    foreach (var item in DataGridCollection)
                    {
                        var line = string.Join(",", item.GetType().GetProperties().Select(prop => prop.GetValue(item, null)));
                        sw.WriteLine(line);
                    }
                }
            }
        }

        private void ClearDataGrid(object param)
        {
            DataGridCollection.Clear();
        }

        private void OpenNewWindow(object param)
        {
            SecondWindow window = new SecondWindow(this); // przekazuje instancję MyViewModel
            window.Show();
        }

    }
}

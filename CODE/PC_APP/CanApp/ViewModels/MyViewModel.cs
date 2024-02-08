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
using CanApp.VIews;
using System.Windows.Controls;


namespace CanApp.ViewModels
{
    public class MyViewModel
    {
        static SerialPort _serialPort;
        public ObservableCollection<MyDataModel> DataGridCollection1 { get; set; }
        public ObservableCollection<MyDataModel> DataGridCollection2 { get; set; }
        public ObservableCollection<MyDataModel> DataGridCollection3 { get; set; }
        public ICommand OpenPortCommand { get; private set; }
        public ICommand ClosePortCommand { get; private set; }
        public ICommand ExportToCsvCommand { get; private set; }
        public ICommand ClearDataGridCommand { get; private set; }
        public ICommand OpenNewWindowCommand { get; private set; }
        public ObservableCollection<int> AvailableFrameIds { get; private set; }

        private Dictionary<int, DateTime> lastReceivedTimestamps = new Dictionary<int, DateTime>();
        public ICommand SendFrameCommand { get; private set; }

        public MyViewModel()
        {
            DataGridCollection1 = new ObservableCollection<MyDataModel>();
            DataGridCollection2 = new ObservableCollection<MyDataModel>();
            DataGridCollection3 = new ObservableCollection<MyDataModel>();
            // Konfiguracja portu szeregowego (przykład, dostosuj do swoich potrzeb)

            OpenPortCommand = new RelayCommand(OpenPort);
            ClosePortCommand = new RelayCommand(ClosePort, CanClosePort);
            ExportToCsvCommand = new RelayCommand(ExportToCsv);
            ClearDataGridCommand = new RelayCommand(ClearDataGrid);
            _serialPort = new SerialPort("COM5", 115200);
            _serialPort.DataReceived += SerialPort_DataReceived;
            OpenNewWindowCommand = new RelayCommand(OpenNewWindow);
            AvailableFrameIds = new ObservableCollection<int>();
            SendFrameCommand = new RelayCommand(obj => SendFrame());
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
                                DataGridCollection1.Add(canData);
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
            //int id = (frameBytes[1] << 8) + frameBytes[2];
            int id = ((frameBytes[2] & 0x07) << 8) | frameBytes[1];
            byte[] dataBytes = frameBytes.Skip(3).Take(dlc).ToArray();

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
                    var headers = DataGridCollection1.FirstOrDefault()?.GetType().GetProperties().Select(prop => prop.Name);
                    var headerLine = string.Join(",", headers);
                    sw.WriteLine(headerLine);

                    // Write data
                    foreach (var item in DataGridCollection1)
                    {
                        var line = string.Join(",", item.GetType().GetProperties().Select(prop => prop.GetValue(item, null)));
                        sw.WriteLine(line);
                    }
                }
            }
        }

        private void ClearDataGrid(object parameter)
        {
            string dataGridName = parameter as string;
            switch (dataGridName)
            {
                case "DataGrid1":
                    DataGridCollection1.Clear();
                    break;
                case "DataGrid2":
                    DataGridCollection2.Clear();
                    break;
                case "DataGrid3":
                    DataGridCollection3.Clear();
                    break;
            }
        }

        private void OpenNewWindow(object param)
        {
            SecondWindow window = new SecondWindow(this); // przekazuje instancję MyViewModel
            window.Show();
        }

        private void SendFrame()
        {
            if (DataGridCollection2.Count > 0)
            {
                var item = DataGridCollection2[0]; // Or get the selected item
                byte[] frame = ConstructFrame(item.ID, item.DLC, item.Bytes);
                SendFrameToSerialPort(frame);
            }
        }

        private byte[] ConstructFrame(int id, int dlc, byte[] dataBytes)
        {
            List<byte> frame = new List<byte> { 0xAA }; // Start byte

            // ID and DLC already integers, directly use them
            frame.Add((byte)(dlc & 0x0F)); // DLC, ensuring only lower 4 bits are used
            frame.Add((byte)(id >> 3)); // First 8 bits of ID
            frame.Add((byte)((id & 0x07) << 5)); // Next 3 bits, shifted


            // Data bytes are already a byte[], directly add them
            frame.AddRange(dataBytes);

            frame.Add(0x55); // End byte
            return frame.ToArray();
        }

        private void SendFrameToSerialPort(byte[] frame)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Write(frame, 0, frame.Length);
            }
        }

        public void OnUserInputBytes(string byteString, MyDataModel model)
        {
            // Assuming 'byteString' is the string from the user, e.g., "11 22"
            // Parse the string into a byte[]
            byte[] parsedBytes = byteString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(b => Convert.ToByte(b, 16))
                                           .ToArray();
            // Update the model
            model.Bytes = parsedBytes;

            // Now you can call SendFrame() to send the data
            SendFrame();
        }



    }
}

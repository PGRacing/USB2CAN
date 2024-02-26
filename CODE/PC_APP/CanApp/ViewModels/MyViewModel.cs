using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using CanApp.VIews;
using System.Windows.Threading;
using System.ComponentModel;


namespace CanApp.ViewModels
{
    public class MyViewModel : INotifyPropertyChanged
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
        public ObservableCollection<string> AvailableFrameIds { get; set; }

        public ICommand SendFrameCommand { get; private set; }
        private readonly object bufferLock = new object();
        private DispatcherTimer uiUpdateTimer;
        private List<byte[]> framesBuffer = new List<byte[]>();
        private CircularBuffer circularBuffer = new CircularBuffer(1024 * 10);
        public ObservableCollection<string> AvailablePorts { get; private set; }
        private string _selectedPort;
        private volatile bool _continueReading = true;
        private HashSet<string> allSeenHexIds = new HashSet<string>();

        private Dictionary<string, ObservableCollection<MyDataModel>> framesById = new Dictionary<string, ObservableCollection<MyDataModel>>();

        public Dictionary<string, ObservableCollection<MyDataModel>> FramesById
        {
             get { return framesById; }
        }




        public MyViewModel()
        {
            DataGridCollection1 = new ObservableCollection<MyDataModel>();
            DataGridCollection1.CollectionChanged += (s, e) => LoadUniqueHexIds();
            DataGridCollection2 = new ObservableCollection<MyDataModel>();
            DataGridCollection3 = new ObservableCollection<MyDataModel>();
            AvailablePorts = new ObservableCollection<string>();
            // Konfiguracja portu szeregowego (przykład, dostosuj do swoich potrzeb)

            OpenPortCommand = new RelayCommand(OpenPort);
            ClosePortCommand = new RelayCommand(ClosePort, CanClosePort);
            ExportToCsvCommand = new RelayCommand(ExportToCsv);
            ClearDataGridCommand = new RelayCommand(ClearDataGrid);
            _serialPort = new SerialPort(); // Początkowo bez przypisanego portu
            _serialPort.DataReceived += SerialPort_DataReceived;
            OpenNewWindowCommand = new RelayCommand(OpenNewWindow);
            AvailableFrameIds = new ObservableCollection<string>();
            SendFrameCommand = new RelayCommand(obj => SendFrame());
            uiUpdateTimer = new DispatcherTimer();
            uiUpdateTimer.Interval = TimeSpan.FromMilliseconds(2);
            uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            uiUpdateTimer.Start();
            LoadAvailablePorts();
        }
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (_continueReading && _serialPort.IsOpen && _serialPort.BytesToRead > 0)
            {
                try
                {
                    var readByte = (byte)_serialPort.ReadByte();
                    // Sprawdzenie, czy kontynuować odczyt i czy port jest otwarty przed dodaniem bajtu do bufora
                    if (_continueReading && _serialPort.IsOpen)
                    {
                        circularBuffer.Add(readByte);
                    }
                    else
                    {
                        break; // Zakończ pętlę, jeśli port nie jest otwarty lub flaga _continueReading jest false
                    }
                }
                catch (InvalidOperationException)
                {
                    break; // Zakończ pętlę, jeśli wystąpi wyjątek (np. port zamknięty)
                }
            }
        }

        private void ProcessBufferData()
        {
            bool packetStartDetected = false;
            int expectedLength = 0;
            List<byte> currentFrameBuffer = new List<byte>();
            int processLimit = 1000; // Ilość bajtów do przetworzenia w jednym cyklu

            while (circularBuffer.Count > 0 && processLimit > 0)
            {
                byte readByte = circularBuffer.Remove(); // Pobierz bajt z bufora cyklicznego
                processLimit--;

                if (!packetStartDetected)
                {
                    if (readByte == 0xAA) // Początek ramki
                    {
                        packetStartDetected = true;
                        currentFrameBuffer.Add(readByte); // Dodaj początek ramki do bufora ramki
                    }
                }
                else // Jeśli początek ramki został wykryty
                {
                    currentFrameBuffer.Add(readByte); // Kontynuuj dodawanie bajtów do ramki

                    if (currentFrameBuffer.Count == 2) // Długość ramki jest określona w drugim bajcie
                    {
                        expectedLength = readByte & 0x0F; // Uzyskaj długość ramki z drugiego bajtu
                        expectedLength += 5; // Dostosuj do własnego formatu (nagłówek + ID + dane + CRC)
                    }

                    // Sprawdź, czy otrzymaliśmy pełną ramkę lub jeśli ramka jest błędna (brak 0x55 na końcu)
                    if ((currentFrameBuffer.Count >= expectedLength && readByte == 0x55) ||
                        (readByte != 0x55 && currentFrameBuffer.Count > expectedLength))
                    {
                        if (currentFrameBuffer.Count >= expectedLength && readByte == 0x55)
                        {
                            lock (bufferLock)
                            {
                                framesBuffer.Add(currentFrameBuffer.ToArray()); // Dodaj kompletną ramkę do bufora
                            }
                            currentFrameBuffer.Clear(); // Wyczyść bufor bieżącej ramki
                            packetStartDetected = false; // Resetuj detekcję początku ramki
                            expectedLength = 0; // Resetuj oczekiwaną długość
                        }
                        // Wyczyść bufor bieżącej ramki na potrzeby następnej ramki, niezależnie czy była błędna, czy nie
                        currentFrameBuffer.Clear();
                        packetStartDetected = false; // Resetuj detekcję początku ramki
                        expectedLength = 0; // Resetuj oczekiwaną długość ramki
                    }
                }
            }
        }

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lock (bufferLock)
                {
                    ProcessBufferData();
                    while (framesBuffer.Count > 0)
                    {
                        var frameData = framesBuffer[0];
                        framesBuffer.RemoveAt(0); // Usuń przetworzoną ramkę z bufora

                        var canData = ConvertBytesToCanFrame(frameData, frameData.Length);
                        if (canData != null)
                        {
                            // Tutaj dodajemy ramkę do słownika
                            AddFrameToDictionary(canData);

                            if (DataGridCollection1.Count >= 1000)
                            {
                                DataGridCollection1.RemoveAt(0); // Usuń najstarszą ramkę, jeśli przekroczono limit
                            }
                            DataGridCollection1.Add(canData); // Dodaj nową ramkę do UI
                        }
                    }
                }
            });
        }


        public void LoadUniqueHexIds()
        {
            // Pobierz aktualne unikalne ID, konwertuj na HEX
            var currentHexIds = DataGridCollection1.Select(frame => frame.ID.ToString("X")).Distinct();

            // Dodaj nowe identyfikatory do zbioru śledzącego wszystkie identyfikatory
            foreach (var hexId in currentHexIds)
            {
                allSeenHexIds.Add(hexId);
            }

            // Posortuj wszystkie kiedykolwiek widziane identyfikatory HEX
            var sortedAllHexIds = allSeenHexIds.OrderBy(hex => int.Parse(hex, System.Globalization.NumberStyles.HexNumber)).ToList();

            // Zaktualizuj AvailableFrameIds tylko jeśli jest to konieczne
            if (!sortedAllHexIds.SequenceEqual(AvailableFrameIds))
            {
                AvailableFrameIds.Clear();
                foreach (var hexId in sortedAllHexIds)
                {
                    AvailableFrameIds.Add(hexId);
                }
            }
        }



        private MyDataModel ConvertBytesToCanFrame(byte[] buffer, int length)
        {
            // Sprawdź minimalną długość ramki
            if (length < 5 || buffer[0] != 0xAA || buffer[length - 1] != 0x55)
            {
                return null; // Nieprawidłowa ramka
            }

            // Zakładając, że format ramki to: 0xAA | DLC | ID (2 bajty) | dane | 0x55
            int dlc = buffer[1] & 0x0F; // Długość danych

            // Sprawdzenie, czy długość ramki jest prawidłowa
            if (length != 5 + dlc) // 1 bajt na nagłówek, 1 na DLC, 2 na ID, X na dane, 1 na kod zakończenia
            {
                return null; // Nieprawidłowa długość ramki
            }

            // Wyodrębnij ID ramki
            int id = (buffer[2] << 8) + buffer[3];

            // Kopiuj dane do nowej tablicy
            byte[] dataBytes = new byte[dlc];
            Array.Copy(buffer, 4, dataBytes, 0, dlc);

            // Utwórz i zwróć instancję MyDataModel
            return new MyDataModel
            {
                ID = id,
                DLC = dlc,
                Bytes = dataBytes
            };
        }



        private void OpenPort(object param)
        {
            if (_serialPort != null && !_serialPort.IsOpen)
            {
                try
                {
                    _continueReading = true; // Wznów odczytywanie danych
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
                    _continueReading = false; // Zatrzymaj odczytywanie danych
                    _serialPort.Close();

                    // Czyszczenie circularBuffer po zamknięciu portu
                    circularBuffer.Clear(); // Zakładam, że masz metodę Clear() w CircularBuffer
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
                    circularBuffer.Clear(); // Zakładam, że masz metodę Clear() w CircularBuffer
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

        private void AddFrameToDictionary(MyDataModel frame)
        {
            string hexId = frame.HexID;

            if (!framesById.ContainsKey(hexId))
            {
                framesById[hexId] = new ObservableCollection<MyDataModel>();
            }

            var framesList = framesById[hexId];
            if (framesList.Count >= 100) // Ogranicz do 100 ramek
            {
                framesList.RemoveAt(0); // Usuń najstarszą ramkę
            }
            framesList.Add(frame); // Dodaj nową ramkę
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

        public string SelectedPort
        {
            get => _selectedPort;
            set
            {
                if (_selectedPort != value)
                {
                    _selectedPort = value;
                    OnPropertyChanged(nameof(SelectedPort));
                    // Aktualizuj _serialPort z nowym wybranym portem
                    UpdateSerialPort(_selectedPort);
                }
            }
        }
        private void LoadAvailablePorts()
        {
            AvailablePorts.Clear();
            var ports = SerialPort.GetPortNames();
            if (ports.Any())
            {
                foreach (var port in ports)
                {
                    AvailablePorts.Add(port);
                }
            }
            else
            {
                // Dodaj tę linię tylko dla celów debugowania, aby zobaczyć, czy lista się pojawia
                AvailablePorts.Add("No COM ports found");
            }
        }

        private void UpdateSerialPort(string portName)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close(); // Zamknij port, jeśli jest otwarty
            }

            _serialPort.PortName = portName; // Zaktualizuj nazwę portu
            _serialPort.BaudRate = 115200; // Możesz dostosować te ustawienia do swoich potrzeb
                                           // Dodaj inne konfiguracje portu szeregowego, jeśli potrzebujesz
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    class CircularBuffer
    {
        private byte[] buffer;
        private int head;
        private int tail;
        private int capacity;
        private bool isFull;

        public CircularBuffer(int capacity)
        {
            this.capacity = capacity;
            buffer = new byte[capacity];
            head = capacity - 1;
        }

        public void Add(byte data)
        {
            head = (head + 1) % capacity;
            buffer[head] = data;

            if (isFull)
            {
                tail = (tail + 1) % capacity;
            }
            else if (head == tail)
            {
                isFull = true;
            }
        }

        public int Capacity => capacity;
        public bool IsFull => isFull;
        public byte Remove()
        {
            if (Count == 0) throw new InvalidOperationException("Buffer is empty");
            byte data = buffer[tail];
            tail = (tail + 1) % capacity;
            isFull = false; // Po usunięciu elementu bufor nie może być pełny
            return data;
        }

        public int Count
        {
            get
            {
                if (isFull) return capacity;
                if (head >= tail) return head - tail + 1;
                return capacity - tail + head + 1;
            }
        }
        public void Clear()
        {
            head = 0;
            tail = 0;
            isFull = false;
            Array.Clear(buffer, 0, buffer.Length); // Wyczyszczenie zawartości bufora
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace kolejna_proba
{
    public partial class MainWindow : Window
    {
        private SerialPort serialPort;
        private CancellationTokenSource readCancellationTokenSource;
        private byte[] buffer = new byte[4096];
        private int bufferStart = 0;
        private int bufferEnd = 0;
        private readonly object bufferLock = new object();
        private List<CanFrame> framesBuffer = new List<CanFrame>();
        private readonly object framesBufferLock = new object();
        private DispatcherTimer uiUpdateTimer;
        private Stopwatch stopwatch = Stopwatch.StartNew();
        private DateTime startTime = DateTime.Now;
        private FramesByIdWindow framesByIdWindow;


        private int frameCounter = 0; // Dodane pole
        private List<CanFrame> allFrames = new List<CanFrame>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeSerialPort();
            InitializeUiUpdateTimer();
            LoadAvailablePorts(); // Dodane
        }

        private void LoadAvailablePorts()
        {
            string[] ports = SerialPort.GetPortNames();
            comboBoxPorts.ItemsSource = ports;
            if (ports.Length > 0)
            {
                comboBoxPorts.SelectedIndex = 0; // Wybierz pierwszy port domyślnie
            }
        }

        private void InitializeSerialPort()
        {
            serialPort = new SerialPort
            {
                // PortName będzie ustawiany podczas otwierania portu
                BaudRate = 250000,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = -1,
                WriteTimeout = -1
            };
        }

        private void InitializeUiUpdateTimer()
        {
            uiUpdateTimer = new DispatcherTimer();
            uiUpdateTimer.Interval = TimeSpan.FromMilliseconds(10); // Aktualizuj UI co 10 ms
            uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            uiUpdateTimer.Start();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSerialPort();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            CloseSerialPort();
        }

        private void OpenSerialPort()
        {
            if (!serialPort.IsOpen)
            {
                if (comboBoxPorts.SelectedItem == null)
                {
                    MessageBox.Show("Proszę wybrać port COM.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                serialPort.PortName = comboBoxPorts.SelectedItem.ToString();

                serialPort.Open();
                readCancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => ReadDataAsync(readCancellationTokenSource.Token));
            }
        }

        private void CloseSerialPort()
        {
            if (serialPort.IsOpen)
            {
                readCancellationTokenSource.Cancel();
                serialPort.Close();
            }
        }

        private async Task ReadDataAsync(CancellationToken cancellationToken)
        {
            byte[] readBuffer = new byte[1024];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = await serialPort.BaseStream.ReadAsync(readBuffer, 0, readBuffer.Length, cancellationToken);
                    if (bytesRead > 0)
                    {
                        // Przypisz znacznik czasu tutaj
                        DateTime receiveTimestamp = startTime.Add(stopwatch.Elapsed);
                        long receiveTicks = Stopwatch.GetTimestamp();

                        ProcessReceivedData(readBuffer, bytesRead, receiveTimestamp, receiveTicks);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Obsługa błędów
                    Console.WriteLine("Błąd odczytu: " + ex.Message);
                }
            }
        }

        private void ProcessReceivedData(byte[] data, int bytesRead, DateTime receiveTimestamp, long receiveTicks)
        {
            lock (bufferLock)
            {
                // Sprawdź, czy mamy wystarczająco miejsca w buforze
                int spaceLeft = buffer.Length - bufferEnd;
                if (bytesRead > spaceLeft)
                {
                    // Przenieś nieprzetworzone dane na początek bufora
                    int unprocessedBytes = bufferEnd - bufferStart;
                    Array.Copy(buffer, bufferStart, buffer, 0, unprocessedBytes);
                    bufferStart = 0;
                    bufferEnd = unprocessedBytes;

                    spaceLeft = buffer.Length - bufferEnd;
                    if (bytesRead > spaceLeft)
                    {
                        // Bufor jest za mały
                        Console.WriteLine("Bufor przepełniony!");
                        return;
                    }
                }

                // Skopiuj nowe dane do bufora
                Array.Copy(data, 0, buffer, bufferEnd, bytesRead);
                bufferEnd += bytesRead;

                // Przechowaj znacznik czasu i Ticks dla tego fragmentu danych
                // Nie jest potrzebne w tej wersji
            }

            // Przetwórz dane z bufora
            ProcessData();
        }

        private void ProcessData()
        {
            while (true)
            {
                if (TryParseFrame(out CanFrame frame))
                {
                    // Dodaj ramkę do bufora do wyświetlenia
                    lock (framesBufferLock)
                    {
                        framesBuffer.Add(frame);
                    }

                    // Dodaj ramkę do listy wszystkich ramek
                    lock (allFrames)
                    {
                        allFrames.Add(frame);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private bool TryParseFrame(out CanFrame frame)
        {
            frame = null;
            lock (bufferLock)
            {
                while (bufferEnd - bufferStart >= 5) // Minimalna długość ramki
                {
                    if (buffer[bufferStart] == 0xAA)
                    {
                        int dlc = buffer[bufferStart + 1] & 0x0F;
                        int frameLength = 5 + dlc;

                        if (bufferEnd - bufferStart >= frameLength)
                        {
                            if (buffer[bufferStart + frameLength - 1] == 0x55)
                            {
                                // Przed utworzeniem ramki pobierz aktualny czas systemowy
                                DateTime frameTimestamp = DateTime.Now;

                                // Mamy pełną ramkę
                                frame = new CanFrame
                                {
                                    SequenceId = ++frameCounter, // Przypisujemy sekwencyjny ID
                                    StartMarker = 0xAA,
                                    DLC = dlc,
                                    ID = (buffer[bufferStart + 2] << 8) | buffer[bufferStart + 3],
                                    EndMarker = 0x55,
                                    Timestamp = frameTimestamp
                                    // Nie potrzebujemy Ticks, jeśli nie używamy Stopwatch
                                };

                                // Odczyt danych
                                for (int i = 0; i < dlc; i++)
                                {
                                    frame.Data.Add(buffer[bufferStart + 4 + i]);
                                }

                                bufferStart += frameLength;

                                // Resetujemy indeksy, jeśli wszystko przetworzyliśmy
                                if (bufferStart == bufferEnd)
                                {
                                    bufferStart = 0;
                                    bufferEnd = 0;
                                }

                                return true;
                            }
                            else
                            {
                                // Nieprawidłowy EndMarker, przesuwamy bufferStart
                                bufferStart++;
                            }
                        }
                        else
                        {
                            // Nie mamy jeszcze pełnej ramki
                            break;
                        }
                    }
                    else
                    {
                        // Szukamy następnego StartMarker
                        bufferStart++;
                    }

                    // Resetujemy indeksy, jeśli wszystko przetworzyliśmy
                    if (bufferStart == bufferEnd)
                    {
                        bufferStart = 0;
                        bufferEnd = 0;
                    }
                }
            }
            return false;
        }


        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            List<CanFrame> framesToDisplay;
            lock (framesBufferLock)
            {
                framesToDisplay = new List<CanFrame>(framesBuffer);
                framesBuffer.Clear();
            }

            foreach (var frame in framesToDisplay)
            {
                // Aktualizuj interfejs użytkownika, np. dodaj do listy
                AddFrameToList(frame);
            }

            // Przekaż nowe ramki do okna FramesByIdWindow
            if (framesByIdWindow != null && framesByIdWindow.IsVisible)
            {
                framesByIdWindow.UpdateFrames(framesToDisplay);
            }
        }

        private void OpenFramesByIdWindow_Click(object sender, RoutedEventArgs e)
        {
            if (framesByIdWindow == null || !framesByIdWindow.IsVisible)
            {
                List<CanFrame> framesCopy;
                lock (allFrames)
                {
                    framesCopy = new List<CanFrame>(allFrames);
                }

                framesByIdWindow = new FramesByIdWindow(framesCopy);
                framesByIdWindow.Show();
            }
            else
            {
                framesByIdWindow.Activate();
            }
        }



        private void AddFrameToList(CanFrame frame)
        {
            dataGridFrames.Items.Add(frame);

            // Pobierz maksymalną liczbę ramek z TextBox
            if (int.TryParse(textBoxMaxFrames.Text, out int maxFrames))
            {
                if (maxFrames > 1000)
                    maxFrames = 1000; // Ustaw maksymalnie 1000

                // Ogranicz liczbę wyświetlanych ramek
                while (dataGridFrames.Items.Count > maxFrames)
                {
                    dataGridFrames.Items.RemoveAt(0);
                }
            }
            else
            {
                // Jeśli wartość nie jest liczbą, ustaw domyślnie 1000
                while (dataGridFrames.Items.Count > 1000)
                {
                    dataGridFrames.Items.RemoveAt(0);
                }
            }
        }

        private void textBoxMaxFrames_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            return int.TryParse(text, out _);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // Sprawdź, czy port jest otwarty
            if (!serialPort.IsOpen)
            {
                MessageBox.Show("Port nie jest otwarty.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Pobierz ID ramki
            if (!int.TryParse(textBoxFrameID.Text, System.Globalization.NumberStyles.HexNumber, null, out int frameID))
            {
                MessageBox.Show("Nieprawidłowy ID ramki. Wprowadź ID w formacie szesnastkowym.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Pobierz dane ramki
            string dataText = textBoxFrameData.Text;
            string[] dataBytesString = dataText.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (dataBytesString.Length > 8)
            {
                MessageBox.Show("Maksymalna liczba danych to 8 bajtów.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            byte[] dataBytes = new byte[dataBytesString.Length];
            for (int i = 0; i < dataBytesString.Length; i++)
            {
                if (!byte.TryParse(dataBytesString[i], System.Globalization.NumberStyles.HexNumber, null, out dataBytes[i]))
                {
                    MessageBox.Show($"Nieprawidłowy bajt danych: {dataBytesString[i]}. Wprowadź dane w formacie szesnastkowym.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Zbuduj ramkę
            byte[] frameBytes = BuildFrame(frameID, dataBytes);

            // Wyślij ramkę
            serialPort.Write(frameBytes, 0, frameBytes.Length);
        }

        private byte[] BuildFrame(int frameID, byte[] data)
        {
            List<byte> frame = new List<byte>();

            // Start Marker
            frame.Add(0xAA);

            // DLC (4 bity) + reszta 4 bitów może być użyta do innych celów
            byte dlc = (byte)(data.Length & 0x0F);
            frame.Add(dlc);

            // ID (2 bajty)
            frame.Add((byte)((frameID >> 8) & 0xFF)); // ID High
            frame.Add((byte)(frameID & 0xFF));        // ID Low

            // Data
            frame.AddRange(data);

            // End Marker
            frame.Add(0x55);

            return frame.ToArray();
        }
    }

    public class CanFrame
    {
        public int SequenceId { get; set; }
        public byte StartMarker { get; set; }
        public int DLC { get; set; }
        public int ID { get; set; }
        public List<byte> Data { get; set; } = new List<byte>();
        public byte EndMarker { get; set; }
        public DateTime Timestamp { get; set; }

        // Właściwość formatująca czas
        public string TimeFormatted => Timestamp.ToString("HH:mm:ss.ffffff");

        // Właściwość DataString
        public string DataString => string.Join(" ", Data.Select(b => b.ToString("X2")));
    }

}

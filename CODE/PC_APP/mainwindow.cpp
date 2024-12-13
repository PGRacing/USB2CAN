#include "mainwindow.h"
#include "ui_mainwindow.h"
#include <QSerialPortInfo>
#include <QSpinBox>
#include <QFileDialog>
#include <QFile>
#include <QTextStream>

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent)
    , ui(new Ui::MainWindow)
    , frameModel(new FrameTableModel(this))
    , worker(nullptr)
    , workerThread(nullptr)
    , maxRows(1000) // Default value
    , filterWindow(nullptr)
    , frameCounter(0) // Initialize frameCounter
{
    ui->setupUi(this);

    ui->tableViewFrames->setModel(frameModel);

    foreach (const QSerialPortInfo &info, QSerialPortInfo::availablePorts()) {
        ui->comboBoxPorts->addItem(info.portName());
    }

    connect(ui->openPortButton, &QPushButton::clicked, this, &MainWindow::openPort);
    connect(ui->closePortButton, &QPushButton::clicked, this, &MainWindow::closePort);
    connect(ui->maxRowsSpinBox, QOverload<int>::of(&QSpinBox::valueChanged), this, &MainWindow::updateMaxRows);
    connect(ui->filterButton, &QPushButton::clicked, this, &MainWindow::openFilterWindow);
    connect(ui->saveFramesButton, &QPushButton::clicked, this, &MainWindow::saveFramesToFile);
    connect(ui->sendFrameButton, &QPushButton::clicked, this, &MainWindow::sendFrame);

}

MainWindow::~MainWindow()
{
    closePort();
    if (filterWindow) {
        delete filterWindow; // Ensure proper cleanup
        filterWindow = nullptr; // Avoid dangling pointer
    }
    delete ui;
}

void MainWindow::openPort()
{
    QString portName = ui->comboBoxPorts->currentText();
    int selectedBaudRate = ui->comboBoxBaudRate->currentText().toInt();

    if (workerThread && workerThread->isRunning()) {

        ui->statusLabel->setText("Port already open.");
        return;
    }

    workerThread = new QThread(this);

    // Start the worker thread
    workerThread->start();
    workerThread->setPriority(QThread::TimeCriticalPriority);

    // Use invokeMethod to create the worker in the worker thread
    QMetaObject::invokeMethod(this, [this, portName,selectedBaudRate]() {
        worker = new SerialWorker(portName, selectedBaudRate);


        // Move the worker to the worker thread
        worker->moveToThread(workerThread);

        // Connect signals and slots
        connect(worker, &SerialWorker::frameReceived, this, &MainWindow::handleFrame, Qt::QueuedConnection);
        // Removed connection to frameModel

        connect(workerThread, &QThread::finished, worker, &QObject::deleteLater);

        // Start the worker
        QMetaObject::invokeMethod(worker, "start", Qt::QueuedConnection);

    }, Qt::QueuedConnection);

    ui->statusLabel->setText("Port otwarty: " + portName);
}

void MainWindow::closePort()
{
    if (workerThread && workerThread->isRunning()) {
        // Stop the worker safely
        QMetaObject::invokeMethod(worker, "stop", Qt::QueuedConnection);

        workerThread->quit();
        workerThread->wait();

        worker = nullptr;
        delete workerThread;
        workerThread = nullptr;

        ui->statusLabel->setText("Port zamknięty.");
    }
}

void MainWindow::handleFrame(const CanFrame &frame)
{
    CanFrame frameWithNumber = frame;
    frameWithNumber.number = ++frameCounter;

    // Add the frame to the frame model
    frameModel->addFrame(frameWithNumber);

    // Limit the number of rows in the model
    if (frameModel->rowCount() > maxRows) {
        frameModel->removeOldestFrame();
    }

    // Store all frames
    allFrames.append(frameWithNumber);

    // Emit the signal for the FilterWindow
    emit newFrameReceived(frameWithNumber);
}

void MainWindow::updateMaxRows()
{
    maxRows = ui->maxRowsSpinBox->value();
}

void MainWindow::openFilterWindow()
{
    if (!filterWindow) {
        filterWindow = new FilterWindow(this);

        // Connect the newFrameReceived signal to the FilterWindow
        connect(this, &MainWindow::newFrameReceived, filterWindow, &FilterWindow::addNewFrame, Qt::QueuedConnection);
    }

    // Pass the frame data to the filter window
    filterWindow->setFrameData(getAllFrames());
    filterWindow->show();
}

QVector<CanFrame> MainWindow::getAllFrames() const
{
    return allFrames;
}

void MainWindow::saveFramesToFile()
{
    QString fileName = QFileDialog::getSaveFileName(this, "Zapisz ramki", "", "CSV Files (*.csv);;All Files (*)");

    if (fileName.isEmpty()) {
        return; // Użytkownik anulował wybór
    }

    QFile file(fileName);
    if (!file.open(QIODevice::WriteOnly | QIODevice::Text)) {
        ui->statusLabel->setText("Błąd: Nie można otworzyć pliku do zapisu.");
        return;
    }

    QTextStream out(&file);

    // Zapisz nagłówki kolumn CSV
    out << "Number,ID,Data,Timestamp\n";

    // Zapisz wszystkie ramki do pliku CSV
    for (const CanFrame &frame : allFrames) {
        out << frame.number << ","
            << frame.id << ","
            << frame.data.toHex() << ","
            << frame.timestamp << "\n";
    }

    file.close();
    ui->statusLabel->setText("Ramki zapisane do pliku CSV: " + fileName);
}


void MainWindow::sendFrame()
{
    if (!worker || !workerThread || !workerThread->isRunning()) {
        ui->statusLabel->setText("Błąd: Port nie jest otwarty.");
        return;
    }

    QString idText = ui->frameIDLineEdit->text();
    QString dataText = ui->frameDataLineEdit->text().remove(' '); // usuwamy spacje jeśli są

    bool ok;
    uint idValue = idText.toUInt(&ok, 16); // wczytujemy ID w formacie hex
    if (!ok || idValue > 0x7FF) {
        ui->statusLabel->setText("Błąd: Nieprawidłowy ID (musi być 11-bit, np. maks 0x7FF).");
        return;
    }

    QByteArray data = QByteArray::fromHex(dataText.toUtf8());
    if (data.size() > 8) {
        ui->statusLabel->setText("Błąd: Maksymalnie 8 bajtów danych.");
        return;
    }

    int dlc = data.size();

    // Budowa ramki wg opisanego formatu
    QByteArray frameToSend;
    frameToSend.append((char)0xAA); // header

    // Type byte: 0xC0 bazowo + dlc
    // bit5=0 standard, bit4=0 data frame
    char typeByte = (char)(0xC0 | (dlc & 0x0F));
    frameToSend.append(typeByte);

    // ID standard 11-bit
    char idLow = (char)(idValue & 0xFF);
    char idHigh = (char)((idValue >> 8) & 0x07);
    frameToSend.append(idLow);
    frameToSend.append(idHigh);

    // Data
    frameToSend.append(data);

    // End code
    frameToSend.append((char)0x55);

    // Wyślij ramkę przez worker
    QMetaObject::invokeMethod(worker, "sendRawBytes", Q_ARG(QByteArray, frameToSend));

    ui->statusLabel->setText("Ramka wysłana: ID=0x" + QString::number(idValue,16).toUpper() + " Data=" + dataText.toUpper());
}

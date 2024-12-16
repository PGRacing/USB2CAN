#include "serialworker.h"
#include <QDateTime>

SerialWorker::SerialWorker(const QString &portName, int baudRate, QObject *parent)
    : QObject(parent), serial(nullptr), running(false), parseState(WaitingForStart)
{
    this->portName = portName;
    this->baudRate = baudRate; // Dodaj pole baudRate w klasie
}
SerialWorker::~SerialWorker()
{
    stop();
}

void SerialWorker::start()
{
    if (!running) {

        serial = new QSerialPort();

        serial->setPortName(portName);
        serial->setBaudRate(baudRate);
        serial->setDataBits(QSerialPort::Data8);
        serial->setParity(QSerialPort::NoParity);
        serial->setStopBits(QSerialPort::OneStop);
        serial->setFlowControl(QSerialPort::NoFlowControl);
        serial->setReadBufferSize(1);

        connect(serial, &QSerialPort::readyRead, this, &SerialWorker::readData);

        if (serial->open(QIODevice::ReadWrite)) {
            running = true;
        } else {
            // Handle error if needed
            delete serial;
            serial = nullptr;
        }
    }
}

void SerialWorker::stop()
{
    running = false;
    if (serial) {
        if (serial->isOpen()) {
            serial->close();
        }
        serial->deleteLater(); // Ensure serial is deleted in the correct thread
        serial = nullptr;
    }
}

void SerialWorker::readData()
{
    if (!running || !serial)
        return;

    QByteArray data = serial->readAll();

    for (int i = 0; i < data.size(); ++i) {
        char byte = data.at(i);

        switch (parseState) {
        case WaitingForStart:
            if (byte == (char)0xAA) {
                currentFrame.clear();
                currentFrame.append(byte);
                parseState = ReadingFrame;
            }
            break;

        case ReadingFrame:
            currentFrame.append(byte);

            if (currentFrame.size() >= 2) {
                // Odczytaj DLC, gdy jest dostępne
                int dlc = currentFrame[1] & 0x0F;
                int expectedLength = 1 + 1 + 2 + dlc + 1; // Początek + DLC + ID + dane + koniec

                if (currentFrame.size() == expectedLength) {
                    // Sprawdź, czy kończy się poprawnym bajtem końca
                    if (byte == (char)0x55) {
                        QDateTime frameTimestamp = QDateTime::currentDateTime();

                        // Parsowanie ramki
                        CanFrame canFrame;
                        canFrame.id = (currentFrame[2]) |
                                      ((currentFrame[3] & 0x07) << 8);
                        canFrame.data = currentFrame.mid(4, dlc);
                        canFrame.timestamp = frameTimestamp.toString("HH:mm:ss.zzz");

                        emit frameReceived(canFrame);
                    }

                    // Reset parsera dla nowej ramki
                    parseState = WaitingForStart;
                } else if (currentFrame.size() > expectedLength) {
                    // Obsługa błędu: ramka zbyt długa
                    parseState = WaitingForStart;
                }
            }
            break;
        }
    }
}

void SerialWorker::sendRawBytes(const QByteArray &data)
{
    if (!serial || !serial->isOpen()) {
        emit errorOccurred("Port szeregowy nie jest otwarty.");
        return;
    }
    serial->write(data);
}

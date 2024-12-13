#ifndef SERIALWORKER_H
#define SERIALWORKER_H

#include <QObject>
#include <QSerialPort>
#include "CanFrame.h"

class SerialWorker : public QObject
{
    Q_OBJECT
public:
    explicit SerialWorker(const QString &portName, int baudRate, QObject *parent = nullptr);
    ~SerialWorker();
    void sendRawBytes(const QByteArray &data);

signals:
    void frameReceived(const CanFrame &frame);
    void errorOccurred(const QString &error);

public slots:
    void start();
    void stop();

private slots:
    void readData();

private:
    QSerialPort *serial;
    QString portName;
    bool running;
    QByteArray currentFrame;
    int baudRate;

    enum ParseState {
        WaitingForStart,
        ReadingFrame
    } parseState;

    static const int MAX_FRAME_SIZE = 256;

};

#endif // SERIALWORKER_H

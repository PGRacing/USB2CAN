#ifndef CANFRAME_H
#define CANFRAME_H

#include <QString>

struct CanFrame {
    int number;          // Frame number
    unsigned int id;              // Frame ID
    QByteArray data;       // Frame data as text
    QString timestamp;   // Frame timestamp as a string
};

#endif // CANFRAME_H

#include "FrameTableModel.h"
#include <QString>
#include <QVariant>
#include <QModelIndex>
#include <QList>
#include <QDebug> // Opcjonalnie do debugowania
#include "CanFrame.h"
#include "FrameTableModel.h"

FrameTableModel::FrameTableModel(QObject *parent)
    : QAbstractTableModel(parent)
{
}

void FrameTableModel::addFrame(const CanFrame &frame) {
    beginInsertRows(QModelIndex(), frames.size(), frames.size());
    frames.append(frame);
    endInsertRows();
}

void FrameTableModel::setFrames(const QVector<CanFrame> &newFrames) {
    beginResetModel();
    frames = newFrames;
    endResetModel();
}

QVector<CanFrame> FrameTableModel::getFrames() const {
    return frames;
}

int FrameTableModel::rowCount(const QModelIndex &parent) const {
    Q_UNUSED(parent);
    return frames.size();
}

int FrameTableModel::columnCount(const QModelIndex &parent) const {
    Q_UNUSED(parent);
    return 4; // Nr, ID, Data, Czas
}

void FrameTableModel::removeFirstFrame() {
    if (!frames.isEmpty()) {
        frames.removeFirst();
    }
}

void FrameTableModel::removeOldestFrame() {
    if (!frames.isEmpty()) {
        beginRemoveRows(QModelIndex(), 0, 0); // Usuń pierwszy wiersz (indeks 0)
        frames.removeFirst();
        endRemoveRows();
    }
}



QVariant FrameTableModel::data(const QModelIndex &index, int role) const {
    if (!index.isValid() || role != Qt::DisplayRole)
        return QVariant();

    const CanFrame &frame = frames[index.row()];
    switch (index.column()) {
    case 0: return frame.number;
    case 1: return QString::number(frame.id, 16).toUpper();
    case 2: return frame.data.toHex(' ').toUpper();
    case 3: return frame.timestamp;
    }
    return QVariant();
}

QVariant FrameTableModel::headerData(int section, Qt::Orientation orientation, int role) const {
    if (role == Qt::DisplayRole && orientation == Qt::Horizontal) {
        switch (section) {
        case 0: return "Nr";
        case 1: return "ID";
        case 2: return "Data";
        case 3: return "Czas";
        }
    }
    return QVariant();
}

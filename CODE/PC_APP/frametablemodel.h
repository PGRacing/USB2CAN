#ifndef FRAMETABLEMODEL_H
#define FRAMETABLEMODEL_H

#include <QAbstractTableModel>
#include "CanFrame.h"

class FrameTableModel : public QAbstractTableModel {
    Q_OBJECT

public:
    explicit FrameTableModel(QObject *parent = nullptr);

    void addFrame(const CanFrame &frame);
    void removeFirstFrame();
    void removeOldestFrame();
    void setFrames(const QVector<CanFrame> &newFrames); // For updating the model
    QVector<CanFrame> getFrames() const; // To retrieve all frames

    // Required overrides
    int rowCount(const QModelIndex &parent = QModelIndex()) const override;
    int columnCount(const QModelIndex &parent = QModelIndex()) const override;
    QVariant data(const QModelIndex &index, int role = Qt::DisplayRole) const override;
    QVariant headerData(int section, Qt::Orientation orientation, int role = Qt::DisplayRole) const override;

private:
    QVector<CanFrame> frames;
};

#endif // FRAMETABLEMODEL_H

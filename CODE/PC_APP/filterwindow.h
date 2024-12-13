#ifndef FILTERWINDOW_H
#define FILTERWINDOW_H

#include <QDialog>
#include "CanFrame.h"
#include "FrameTableModel.h"
#include "ui_filterwindow.h"

class FilterWindow : public QDialog
{
    Q_OBJECT

public:
    explicit FilterWindow(QWidget *parent = nullptr);
    ~FilterWindow();

    void setFrameData(const QVector<CanFrame> &frames);
    void addNewFrame(const CanFrame &frame);

private slots:
    void idListWidgetSelectionChanged();
    void dataButtonClicked(int index); // Dodany slot


private:
    Ui::FilterWindow *ui;
    FrameTableModel *filteredFrameModel;

    QVector<CanFrame> allFrames;
    QMap<int, QVector<CanFrame>> framesById;
};

#endif // FILTERWINDOW_H

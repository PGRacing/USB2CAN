#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMainWindow>
#include <QThread>
#include "frametablemodel.h"
#include "serialworker.h"
#include "filterwindow.h"

namespace Ui {
class MainWindow;
}

class MainWindow : public QMainWindow
{
    Q_OBJECT

public:
    explicit MainWindow(QWidget *parent = nullptr);
    ~MainWindow();

signals:
    void newFrameReceived(const CanFrame &frame);

private slots:
    void openPort();
    void closePort();
    void handleFrame(const CanFrame &frame);
    void updateMaxRows();
    void openFilterWindow();
    void saveFramesToFile();
    void sendFrame();

private:
    Ui::MainWindow *ui;
    FrameTableModel *frameModel;
    SerialWorker *worker;
    QThread *workerThread;
    int maxRows;

    FilterWindow *filterWindow;

    QVector<CanFrame> getAllFrames() const;

    int frameCounter; // Moved frameCounter here
    QVector<CanFrame> allFrames; // Stores all frames ever received
};

#endif // MAINWINDOW_H

#ifndef DATAWINDOW_H
#define DATAWINDOW_H

#include <QDialog>
#include <QVector>
#include <QPair>
#include <QtCharts/QChartView>
#include <QtCharts/QLineSeries>
#include <QtCharts/QChart>

QT_BEGIN_NAMESPACE
namespace Ui { class DataWindow; }
QT_END_NAMESPACE

class DataWindow : public QDialog
{
    Q_OBJECT

public:
    explicit DataWindow(int id, int dataIndex, const QVector<QPair<char, QString>> &dataEntries, QWidget *parent = nullptr);
    ~DataWindow();

private:
    Ui::DataWindow *ui;
    int id;
    int dataIndex;
    QVector<QPair<char, QString>> dataEntries;

    QChart *chart; // Wykres
    QChartView *chartView; // Widok wykresu
    QLineSeries *series; // Seria danych
};

#endif // DATAWINDOW_H

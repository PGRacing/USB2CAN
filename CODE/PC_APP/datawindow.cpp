#include "datawindow.h"
#include "ui_datawindow.h"
#include <QtCharts/QValueAxis>
#include <QGraphicsScene>
#include <QDateTime>
#include <QPainter>
#include <QVBoxLayout> // Dodane dla lepszego zarządzania layoutem

DataWindow::DataWindow(int id, int dataIndex, const QVector<QPair<char, QString>> &dataEntries, QWidget *parent) :
    QDialog(parent),
    ui(new Ui::DataWindow),
    id(id),
    dataIndex(dataIndex),
    dataEntries(dataEntries),
    chart(new QChart()),
    chartView(new QChartView(chart)),
    series(new QLineSeries())
{
    ui->setupUi(this);

    // Ustawienie tytułu okna
    setWindowTitle(QString("ID %1 - Data %2").arg(id, 0, 16).arg(dataIndex));

    // Konfiguracja danych wykresu
    int timestamp = 0;
    for (const auto &entry : dataEntries) {
        double dataValue = static_cast<unsigned char>(entry.first);
        series->append(timestamp++, dataValue); // Dodajemy dane do serii
    }

    // Dodanie serii do wykresu
    chart->addSeries(series);
    chart->setTitle("Wykres danych");
    // chart->createDefaultAxes(); // Usuń to wywołanie

    // Konfiguracja osi X (timestamp)
    QValueAxis *axisX = new QValueAxis();
    axisX->setTitleText("Timestamp");
    axisX->setTickCount(10); // Możesz dostosować liczbę znaczników
    axisX->setLabelFormat("%d"); // Format etykiet
    chart->addAxis(axisX, Qt::AlignBottom);
    series->attachAxis(axisX);

    // Konfiguracja osi Y (wartość danych)
    QValueAxis *axisY = new QValueAxis();
    axisY->setTitleText("Wartość Data");
    axisY->setLabelFormat("%.0f");
    axisY->setRange(0, 255); // Zakres dla danych typu char
    axisY->setTickCount(6); // Możesz dostosować liczbę znaczników
    chart->addAxis(axisY, Qt::AlignLeft);
    series->attachAxis(axisY);

    // Konfiguracja widoku wykresu
    chartView->setRenderHint(QPainter::Antialiasing);
    chartView->setSizePolicy(QSizePolicy::Expanding, QSizePolicy::Expanding); // Upewnij się, że wykres się rozciąga

    // Ustawienie layoutu, aby wykres zajmował całe okno
    QVBoxLayout *layout = new QVBoxLayout(this);
    layout->addWidget(chartView);
    setLayout(layout);

    // Opcjonalnie: Ustawienie okna na pełny ekran
    this->showMaximized(); // Lub użyj this->showFullScreen(); dla pełnego ekranu bez pasków

    // Jeśli nadal używasz QGraphicsView, upewnij się, że rozciąga się poprawnie
    /*
    ui->graphicsView->setScene(new QGraphicsScene(this));
    ui->graphicsView->scene()->addWidget(chartView);
    ui->graphicsView->setSizePolicy(QSizePolicy::Expanding, QSizePolicy::Expanding);
    ui->graphicsView->fitInView(chartView->scene()->itemsBoundingRect(), Qt::KeepAspectRatio);
    */
}

DataWindow::~DataWindow()
{
    delete ui;
}

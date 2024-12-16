#include "filterwindow.h"
#include <QSet>
#include <QListWidgetItem>
#include <QDebug>
#include <QMessageBox>
#include "datawindow.h"

FilterWindow::FilterWindow(QWidget *parent)
    : QDialog(parent),
    ui(new Ui::FilterWindow),
    filteredFrameModel(new FrameTableModel(this))
{
    ui->setupUi(this);

    // Check that UI elements are valid
    Q_ASSERT(ui->idListWidget != nullptr);
    Q_ASSERT(ui->frameTableView != nullptr);

    // Set up the ID list widget
    ui->idListWidget->setSelectionMode(QAbstractItemView::SingleSelection);
    connect(ui->idListWidget, &QListWidget::itemSelectionChanged, this, &FilterWindow::idListWidgetSelectionChanged);

    // Set up the frame table view
    ui->frameTableView->setModel(filteredFrameModel);

    // Połączenie przycisków z slotem
    connect(ui->dataButton1, &QPushButton::clicked, this, [this]() { dataButtonClicked(0); });
    connect(ui->dataButton2, &QPushButton::clicked, this, [this]() { dataButtonClicked(1); });
    connect(ui->dataButton3, &QPushButton::clicked, this, [this]() { dataButtonClicked(2); });
    connect(ui->dataButton4, &QPushButton::clicked, this, [this]() { dataButtonClicked(3); });
    connect(ui->dataButton5, &QPushButton::clicked, this, [this]() { dataButtonClicked(4); });
    connect(ui->dataButton6, &QPushButton::clicked, this, [this]() { dataButtonClicked(5); });
    connect(ui->dataButton7, &QPushButton::clicked, this, [this]() { dataButtonClicked(6); });
    connect(ui->dataButton8, &QPushButton::clicked, this, [this]() { dataButtonClicked(7); });
}

FilterWindow::~FilterWindow()
{
    delete ui;
}

void FilterWindow::setFrameData(const QVector<CanFrame> &frames)
{
    allFrames = frames;

    // Build the list of unique IDs
    QSet<int> uniqueIds;
    framesById.clear();

    for (const CanFrame &frame : frames) {
        uniqueIds.insert(frame.id);
        framesById[frame.id].append(frame);
    }

    // Populate the ID list widget
    ui->idListWidget->clear();
    for (int id : uniqueIds) {
        QListWidgetItem *item = new QListWidgetItem(QString::number(id, 16).toUpper());
        item->setData(Qt::UserRole, id); // Store the ID as data
        ui->idListWidget->addItem(item);
    }
}

void FilterWindow::idListWidgetSelectionChanged()
{
    QList<QListWidgetItem *> selectedItems = ui->idListWidget->selectedItems();
    if (selectedItems.isEmpty())
        return;

    int selectedId = selectedItems.first()->data(Qt::UserRole).toInt();

    // Get the last 100 frames with the selected ID
    QVector<CanFrame> framesForId = framesById.value(selectedId);
    int count = framesForId.size();
    QVector<CanFrame> last100Frames = framesForId.mid(qMax(0, count - 100), 100);

    // Update the model
    filteredFrameModel->setFrames(last100Frames);
}

void FilterWindow::addNewFrame(const CanFrame &frame)
{
    int id = frame.id;
    framesById[id].append(frame);

    // If the ID is new, add it to the list
    if (framesById[id].size() == 1) {
        QListWidgetItem *item = new QListWidgetItem(QString::number(id, 16).toUpper());
        item->setData(Qt::UserRole, id);
        ui->idListWidget->addItem(item);
    }

    // If this ID is currently selected, update the table
    QList<QListWidgetItem *> selectedItems = ui->idListWidget->selectedItems();
    if (!selectedItems.isEmpty()) {
        int selectedId = selectedItems.first()->data(Qt::UserRole).toInt();
        if (selectedId == id) {
            // Update the model with the new frame
            QVector<CanFrame> framesForId = framesById.value(id);
            int count = framesForId.size();
            QVector<CanFrame> last100Frames = framesForId.mid(qMax(0, count - 100), 100);
            filteredFrameModel->setFrames(last100Frames);
        }
    }
}

void FilterWindow::dataButtonClicked(int index)
{
    qDebug() << "dataButtonClicked called with index:" << index;

    // Sprawdzenie, czy ID jest wybrane
    QList<QListWidgetItem *> selectedItems = ui->idListWidget->selectedItems();
    if (selectedItems.isEmpty()) {
        QMessageBox::warning(this, tr("Brak wybranego ID"), tr("Proszę wybrać ID z listy."));
        return;
    }

    int selectedId = selectedItems.first()->data(Qt::UserRole).toInt();

    // Pobranie wszystkich ramek dla wybranego ID
    QVector<CanFrame> framesForId = framesById.value(selectedId);

    // Pobranie ostatnich 100 ramek
    int count = framesForId.size();
    QVector<CanFrame> last100Frames = framesForId.mid(qMax(0, count - 100), qMin(100, count));

    // Ekstrakcja wartości Data i timestamp dla konkretnego bajtu
    QVector<QPair<char, QString>> dataAndTimestamps;
    for (const CanFrame &frame : last100Frames) {
        if (index < frame.data.size()) {
            char dataValue = frame.data.at(index);
            QString timestamp = frame.timestamp;
            dataAndTimestamps.append(qMakePair(dataValue, timestamp));
        }
    }

    // Sprawdzenie, czy są dane do wyświetlenia
    if (dataAndTimestamps.isEmpty()) {
        QMessageBox::information(this, tr("Brak danych"), tr("Nie ma danych do wyświetlenia dla wybranego bajtu."));
        return;
    }

    // Utworzenie i otwarie DataWindow
    DataWindow *dataWindow = new DataWindow(selectedId, index + 1, dataAndTimestamps, this);
    dataWindow->setAttribute(Qt::WA_DeleteOnClose); // Automatyczne usuwanie po zamknięciu
    dataWindow->show();
}

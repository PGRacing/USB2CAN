using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CanApp.ViewModels;

namespace CanApp
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MyViewModel();
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var viewModel = DataContext as MyViewModel;
                var dataGrid = sender as DataGrid;

                // Assuming the column for byte input is named "Bytes" in your DataGrid
                if (dataGrid != null && e.Column.Header.ToString() == "Bytes")
                {
                    // Assuming that the DataContext for the DataGrid rows is MyDataModel
                    var editingTextBox = e.EditingElement as TextBox;
                    var item = e.Row.Item as MyDataModel;
                    if (viewModel != null && editingTextBox != null && item != null)
                    {
                        viewModel.OnUserInputBytes(editingTextBox.Text, item);
                    }
                }
            }
        }
    }
}

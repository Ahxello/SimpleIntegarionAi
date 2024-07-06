using System.Windows;
using System.Windows.Controls;
using SimpleIntegrationAi.Domain.Models;
using SimpleIntegrationAi.WPF.ViewModels;

namespace SimpleIntegrationAi.WPF.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.Header = e.PropertyName;
        }
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is Entity selectedEntity)
            {
                var viewModel = DataContext as MainWindowViewModel;
                if (viewModel != null)
                {
                    viewModel.SelectedTable = selectedEntity.Name;
                }
            }
        }
    }
}
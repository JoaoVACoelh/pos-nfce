using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace PosNfce.Desktop.Catalog.Ibpt;

public partial class IbptView : UserControl
{
    public IbptView()
    {
        InitializeComponent();
    }

    private IbptViewModel ViewModel => (IbptViewModel)DataContext;

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ViewModel.LoadAsync();

    private void OnChooseFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Tabela IBPT", Filter = "Tabela IBPT (*.csv)|*.csv" };

        if (dialog.ShowDialog() == true)
        {
            ViewModel.FilePath = dialog.FileName;
        }
    }
}

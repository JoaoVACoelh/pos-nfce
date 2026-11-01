using System.Windows;
using System.Windows.Controls;

namespace PosNfce.Desktop.Fiscal.Printer;

public partial class PrinterView : UserControl
{
    public PrinterView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ((PrinterViewModel)DataContext).LoadAsync();
}

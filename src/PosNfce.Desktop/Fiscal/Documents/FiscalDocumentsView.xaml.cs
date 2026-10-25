using System.Windows;
using System.Windows.Controls;

namespace PosNfce.Desktop.Fiscal.Documents;

public partial class FiscalDocumentsView : UserControl
{
    public FiscalDocumentsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ((FiscalDocumentsViewModel)DataContext).LoadAsync();
}

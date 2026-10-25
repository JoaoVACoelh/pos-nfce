using System.Windows;
using System.Windows.Controls;

namespace PosNfce.Desktop.Fiscal.IbsCbs;

public partial class IbsCbsRatesView : UserControl
{
    public IbsCbsRatesView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await ((IbsCbsRatesViewModel)DataContext).LoadAsync();
}

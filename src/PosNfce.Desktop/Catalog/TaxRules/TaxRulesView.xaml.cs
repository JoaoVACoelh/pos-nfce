using System.Windows;
using System.Windows.Controls;

namespace PosNfce.Desktop.Catalog.TaxRules;

public partial class TaxRulesView : UserControl
{
    public TaxRulesView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ((TaxRulesViewModel)DataContext).LoadAsync();
        NameBox.Focus();
    }
}

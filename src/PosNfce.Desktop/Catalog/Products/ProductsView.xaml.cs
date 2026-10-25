using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PosNfce.Desktop.Catalog.Products;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ((ProductsViewModel)DataContext).LoadAsync();
        CodeBox.Focus();
    }

    // The scanner types the barcode and presses Enter: move on to the description instead of doing anything else.
    private void OnGtinKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DescriptionBox.Focus();
            e.Handled = true;
        }
    }
}

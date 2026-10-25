using System.Windows;
using System.Windows.Controls;
using PosNfce.Fiscal.Application.Documents;

namespace PosNfce.Desktop.Fiscal.Documents;

public partial class FiscalDocumentsView : UserControl
{
    public FiscalDocumentsView()
    {
        InitializeComponent();
    }

    private FiscalDocumentsViewModel ViewModel => (FiscalDocumentsViewModel)DataContext;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowDanfceRequested -= OnShowDanfce;
        ViewModel.ShowDanfceRequested += OnShowDanfce;
        await ViewModel.LoadAsync();
    }

    private void OnShowDanfce(Danfce danfce) => new DanfceWindow(danfce) { Owner = Window.GetWindow(this) }.ShowDialog();
}

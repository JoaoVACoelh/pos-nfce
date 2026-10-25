using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PosNfce.Desktop.Checkout;

public partial class CheckoutView : UserControl
{
    public CheckoutView()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private CheckoutViewModel ViewModel => (CheckoutViewModel)DataContext;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.FocusRequested -= OnFocusRequested;
        ViewModel.FocusRequested += OnFocusRequested;
        await ViewModel.LoadAsync();
    }

    // The ViewModel says where the keyboard should go; Background priority lets bindings (e.g. Visibility) update first.
    private void OnFocusRequested(CheckoutFocus target) => Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
    {
        TextBox box = target switch
        {
            CheckoutFocus.Discount => DiscountBox,
            CheckoutFocus.PaymentAmount => PaymentAmountBox,
            _ => ScanBox,
        };
        box.Focus();
        box.SelectAll();
    });

    private void OnScanKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                ViewModel.MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Down:
                ViewModel.MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Delete when ScanBox.Text.Length == 0:
                ViewModel.RemoveSelectedItemCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    // F4 asks for confirmation here: a dialog is a view concern. The default button is "No", so a stray Enter doesn't cancel.
    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F4 || !ViewModel.HasItems)
        {
            return;
        }

        e.Handled = true;
        var answer = MessageBox.Show(
            "Cancelar a venda inteira?", "Cancelar venda", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer == MessageBoxResult.Yes)
        {
            await ViewModel.CancelSaleAsync();
        }
        else
        {
            ScanBox.Focus();
        }
    }
}

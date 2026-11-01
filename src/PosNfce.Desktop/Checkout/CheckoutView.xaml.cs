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
        ViewModel.CancelRequested -= OnCancelRequested;
        ViewModel.CancelRequested += OnCancelRequested;
        await ViewModel.LoadAsync();
    }

    // The ViewModel says where the keyboard should go; Background priority lets bindings (e.g. Visibility) update first.
    private void OnFocusRequested(CheckoutFocus target) => Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
    {
        TextBox box = target switch
        {
            CheckoutFocus.Discount => DiscountBox,
            CheckoutFocus.Cpf => CpfBox,
            CheckoutFocus.Quantity => QuantityBox,
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
            // ← → only pick the option while nothing is typed; with a code they still move the cursor.
            case Key.Left when ScanBox.Text.Length == 0:
                ViewModel.MoveHighlight(-1);
                e.Handled = true;
                break;
            case Key.Right when ScanBox.Text.Length == 0:
                ViewModel.MoveHighlight(1);
                e.Handled = true;
                break;
            case Key.Delete when ScanBox.Text.Length == 0:
                ViewModel.RemoveSelectedItemCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    // ↑ ↓ pick the payment method (the list is vertical); ← → still move the cursor in the amount.
    private void OnPaymentAmountKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down)
        {
            ViewModel.MovePaymentMethod(e.Key == Key.Up ? -1 : 1);
            e.Handled = true;
        }
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F4 || !ViewModel.HasItems)
        {
            return;
        }

        e.Handled = true;
        await ConfirmCancelAsync();
    }

    private async void OnCancelClick(object sender, RoutedEventArgs e) => await ConfirmCancelAsync();

    private async void OnCancelRequested() => await ConfirmCancelAsync();

    // A dialog is a view concern. The default button is "No", so a stray Enter doesn't cancel the sale.
    private async Task ConfirmCancelAsync()
    {
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

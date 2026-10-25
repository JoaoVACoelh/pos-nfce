using System.Globalization;
using System.Windows.Data;
using PosNfce.Checkout.Domain.Sales;

namespace PosNfce.Desktop.Checkout;

/// <summary>Shows a payment method in Portuguese ("Cartão de débito").</summary>
public sealed class PaymentMethodLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is PaymentMethod method ? CheckoutViewModel.Label(method) : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

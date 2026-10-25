using System.Globalization;
using System.Windows.Data;

namespace PosNfce.Desktop.Shared;

/// <summary>Returns "Selected" when the bound value equals the parameter; used with the OptionButton style's Tag trigger.</summary>
public sealed class SelectedWhenEqualConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Equals(value, parameter) ? "Selected" : null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

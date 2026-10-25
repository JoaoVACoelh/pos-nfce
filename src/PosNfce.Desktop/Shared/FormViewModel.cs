using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Shared;

/// <summary>Feedback line shown under a form, and the conversions every form needs.</summary>
public abstract partial class FormViewModel : ObservableObject
{
    protected static readonly CultureInfo Brazilian = CultureInfo.GetCultureInfo("pt-BR");

    [ObservableProperty]
    public partial string Message { get; set; } = "";

    [ObservableProperty]
    public partial bool IsError { get; set; }

    protected void ShowSuccess(string message) => (Message, IsError) = (message, false);

    protected void ShowError(string message) => (Message, IsError) = (message, true);

    /// <summary>Accepts "9,90", "1.234,50" or "9.90"; the screen is used by people who type either.</summary>
    protected static decimal ParseDecimal(string text, string field)
    {
        var value = text.Trim();
        if (decimal.TryParse(value, NumberStyles.Number, Brazilian, out var result) && !LooksLikeDotDecimal(value))
        {
            return result;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        throw new DomainException($"{field} inválido: use números, por exemplo 9,90.");
    }

    // "9.90" parses in pt-BR as 990 (dot as thousands separator). A single dot followed by 1, 2 or 4 digits is a decimal point.
    private static bool LooksLikeDotDecimal(string value)
    {
        var dot = value.IndexOf('.');
        return dot >= 0 && !value.Contains(',') && value.LastIndexOf('.') == dot && value.Length - dot - 1 is 1 or 2 or 4;
    }
}

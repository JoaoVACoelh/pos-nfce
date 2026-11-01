using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Domain.Sales;

/// <summary>
/// Barcode printed by a labeling scale: EAN-13 "2 CCCCCC WWWWW D" = in-store prefix 2, the product's code in the scale
/// (zero-padded), the weight in grams and the check digit. Example: 2000005003646 = code 5, 0,364 kg.
/// Only weight labels are read; a scale set to print the price inside needs another format.
/// </summary>
public sealed record ScaleLabel(int ItemCode, decimal WeightKg)
{
    public static bool TryParse(string code, [NotNullWhen(true)] out ScaleLabel? label)
    {
        label = null;
        if (code.Length != 13 || code[0] != '2' || !code.All(char.IsAsciiDigit) || CheckDigit(code[..^1]) != code[^1] - '0')
        {
            return false;
        }

        var grams = int.Parse(code[7..12], CultureInfo.InvariantCulture);
        label = new ScaleLabel(int.Parse(code[1..7], CultureInfo.InvariantCulture), grams / 1000m);
        return true;
    }

    public static bool SoldByWeight(string unit) => unit.Equals("KG", StringComparison.OrdinalIgnoreCase);

    /// <summary>The label's weight as the item quantity, as long as the product is sold by the kilogram.</summary>
    public decimal QuantityFor(string unit, string description) =>
        SoldByWeight(unit)
            ? WeightKg
            : throw new DomainException(
                $"A etiqueta da balança traz o peso, mas \"{description}\" está cadastrado em {unit}. Cadastre-o em KG.");

    /// <summary>GS1 mod-10: weights 3 and 1 alternate, starting with 3 on the rightmost digit.</summary>
    private static int CheckDigit(string digitsWithoutCheck)
    {
        var sum = 0;
        for (var i = 0; i < digitsWithoutCheck.Length; i++)
        {
            var digit = digitsWithoutCheck[^(i + 1)] - '0';
            sum += i % 2 == 0 ? digit * 3 : digit;
        }

        return (10 - sum % 10) % 10;
    }
}

using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.Products;

/// <summary>
/// Barcode (GTIN-8, 12, 13 or 14) as sent in the NFC-e <c>cEAN</c> tag, or "SEM GTIN" when the product has none.
/// </summary>
public sealed record Gtin
{
    public const string NoGtinText = "SEM GTIN";

    private static readonly int[] ValidLengths = [8, 12, 13, 14];

    public static Gtin None { get; } = new(NoGtinText);

    public string Value { get; }

    public bool HasBarcode => Value != NoGtinText;

    private Gtin(string value) => Value = value;

    public static Gtin Parse(string? input)
    {
        var value = input?.Trim() ?? "";

        if (value.Length == 0 || value.Equals(NoGtinText, StringComparison.OrdinalIgnoreCase))
        {
            return None;
        }

        if (!value.All(char.IsAsciiDigit) || !ValidLengths.Contains(value.Length))
        {
            throw new DomainException($"GTIN \"{value}\" inválido: deve ter 8, 12, 13 ou 14 dígitos.");
        }

        if (CheckDigit(value[..^1]) != value[^1] - '0')
        {
            throw new DomainException($"GTIN \"{value}\" inválido: dígito verificador não confere.");
        }

        return new Gtin(value);
    }

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

    public override string ToString() => Value;
}

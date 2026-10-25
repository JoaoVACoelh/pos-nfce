using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.Products;

/// <summary>Tax substitution code (CEST), 7 digits. Only products under tax substitution have one.</summary>
public sealed record Cest
{
    public string Value { get; }

    private Cest(string value) => Value = value;

    /// <summary>Returns null when the input is empty: CEST is optional.</summary>
    public static Cest? ParseOptional(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var value = Ncm.DigitsOnly(input);

        if (value.Length != 7 || !value.All(char.IsAsciiDigit))
        {
            throw new DomainException($"CEST \"{input}\" inválido: deve ter 7 dígitos.");
        }

        return new Cest(value);
    }

    public override string ToString() => Value;
}

using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.Products;

/// <summary>Mercosur product classification (NCM), 8 digits. Only the format is checked here; the code comes from the accountant.</summary>
public sealed record Ncm
{
    public string Value { get; }

    private Ncm(string value) => Value = value;

    public static Ncm Parse(string? input)
    {
        var value = DigitsOnly(input);

        if (value.Length != 8 || !value.All(char.IsAsciiDigit))
        {
            throw new DomainException($"NCM \"{input}\" inválido: deve ter 8 dígitos.");
        }

        return new Ncm(value);
    }

    /// <summary>Accepts the usual "0000.00.00" formatting.</summary>
    internal static string DigitsOnly(string? input) =>
        new((input ?? "").Where(c => c != '.' && !char.IsWhiteSpace(c)).ToArray());

    public override string ToString() => Value;
}

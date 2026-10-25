using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.TaxRules;

/// <summary>
/// Operation code (CFOP), 4 digits. NFC-e only covers sales inside the state, so the code must start with 5.
/// Which CFOP each product uses comes from the accountant.
/// </summary>
public sealed record Cfop
{
    public string Value { get; }

    private Cfop(string value) => Value = value;

    public static Cfop Parse(string? input)
    {
        var value = (input ?? "").Replace(".", "").Trim();

        if (value.Length != 4 || !value.All(char.IsAsciiDigit))
        {
            throw new DomainException($"CFOP \"{input}\" inválido: deve ter 4 dígitos.");
        }

        if (value[0] != '5')
        {
            throw new DomainException($"CFOP {value} inválido para NFC-e: a NFC-e só é usada dentro do estado (CFOP 5.xxx).");
        }

        return new Cfop(value);
    }

    public override string ToString() => Value;
}

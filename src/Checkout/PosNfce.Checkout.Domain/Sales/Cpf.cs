using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Domain.Sales;

/// <summary>Customer's CPF, 11 digits with two mod-11 check digits. Optional on an NFC-e ("CPF na nota").</summary>
public sealed record Cpf
{
    public string Value { get; }

    private Cpf(string value) => Value = value;

    public static Cpf Parse(string? input)
    {
        var value = new string((input ?? "").Where(char.IsAsciiDigit).ToArray());

        if (value.Length != 11 || value.Distinct().Count() == 1)
        {
            throw new DomainException($"CPF \"{input}\" inválido: deve ter 11 dígitos.");
        }

        if (CheckDigit(value[..9]) != value[9] - '0' || CheckDigit(value[..10]) != value[10] - '0')
        {
            throw new DomainException($"CPF \"{input}\" inválido: dígito verificador não confere.");
        }

        return new Cpf(value);
    }

    /// <summary>Weights start at length + 1 and go down to 2; remainder below 2 means 0.</summary>
    private static int CheckDigit(string digits)
    {
        var sum = digits.Select((c, i) => (c - '0') * (digits.Length + 1 - i)).Sum();
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    /// <summary>000.000.000-00</summary>
    public string Formatted => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    public override string ToString() => Value;
}

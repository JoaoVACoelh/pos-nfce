using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>Company tax ID, 14 digits with two mod-11 check digits.</summary>
public sealed record Cnpj
{
    private static readonly int[] FirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public string Value { get; }

    /// <summary>First 8 digits: identify the company; the rest identify the branch.</summary>
    public string Root => Value[..8];

    private Cnpj(string value) => Value = value;

    public static Cnpj Parse(string? input)
    {
        var value = new string((input ?? "").Where(char.IsAsciiDigit).ToArray());

        if (value.Length != 14 || value.Distinct().Count() == 1)
        {
            throw new DomainException($"CNPJ \"{input}\" inválido: deve ter 14 dígitos.");
        }

        if (CheckDigit(value[..12], FirstWeights) != value[12] - '0' || CheckDigit(value[..13], SecondWeights) != value[13] - '0')
        {
            throw new DomainException($"CNPJ \"{input}\" inválido: dígito verificador não confere.");
        }

        return new Cnpj(value);
    }

    private static int CheckDigit(string digits, int[] weights)
    {
        var sum = digits.Select((c, i) => (c - '0') * weights[i]).Sum();
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    /// <summary>00.000.000/0000-00</summary>
    public string Formatted => $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";

    public override string ToString() => Value;
}

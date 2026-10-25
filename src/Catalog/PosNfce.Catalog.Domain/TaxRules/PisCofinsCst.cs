using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.TaxRules;

/// <summary>PIS or COFINS situation code (CST). Both taxes share the same code table in the NF-e layout.</summary>
public sealed record PisCofinsCst
{
    private static readonly int[] LayoutCodes =
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9,
        49, 50, 51, 52, 53, 54, 55, 56,
        60, 61, 62, 63, 64, 65, 66, 67,
        70, 71, 72, 73, 74, 75,
        98, 99,
    ];

    public string Value { get; }

    private PisCofinsCst(string value) => Value = value;

    public static PisCofinsCst Parse(string? input, string tax)
    {
        var value = input?.Trim() ?? "";

        if (value.Length != 2 || !int.TryParse(value, out var code) || !LayoutCodes.Contains(code))
        {
            throw new DomainException($"CST de {tax} \"{input}\" inválido.");
        }

        return new PisCofinsCst(value);
    }

    public override string ToString() => Value;
}

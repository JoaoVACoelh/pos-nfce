using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.TaxRules;

/// <summary>Tax rate in percent (1.65 means 1.65%). The NF-e layout allows up to 4 decimal places.</summary>
public readonly record struct Percentage
{
    public decimal Value { get; }

    private Percentage(decimal value) => Value = value;

    public static Percentage Zero { get; } = new(0m);

    public static Percentage Of(decimal value, string field)
    {
        if (value is < 0m or > 100m)
        {
            throw new DomainException($"Alíquota de {field} deve estar entre 0 e 100%.");
        }

        if (decimal.Round(value, 4) != value)
        {
            throw new DomainException($"Alíquota de {field} pode ter no máximo 4 casas decimais.");
        }

        return new Percentage(value);
    }

    public override string ToString() => $"{Value:0.####}%";
}

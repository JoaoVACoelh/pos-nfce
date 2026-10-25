using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.TaxRules;

/// <summary>ICMS situation code for Simples Nacional issuers (CSOSN, CRT 1). Only the codes defined in the NF-e layout are accepted.</summary>
public sealed record Csosn
{
    private static readonly string[] LayoutCodes = ["101", "102", "103", "201", "202", "203", "300", "400", "500", "900"];

    public string Value { get; }

    private Csosn(string value) => Value = value;

    public static Csosn Parse(string? input)
    {
        var value = input?.Trim() ?? "";

        if (!LayoutCodes.Contains(value))
        {
            throw new DomainException($"CSOSN \"{input}\" inválido. Valores aceitos: {string.Join(", ", LayoutCodes)}.");
        }

        return new Csosn(value);
    }

    public override string ToString() => Value;
}

using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>State (UF) with its IBGE code, sent in the NFC-e <c>cUF</c> tag and used as the prefix of city codes.</summary>
public sealed record BrazilianState(string Code, int IbgeCode)
{
    public static IReadOnlyList<BrazilianState> All { get; } =
    [
        new("AC", 12), new("AL", 27), new("AM", 13), new("AP", 16), new("BA", 29), new("CE", 23), new("DF", 53),
        new("ES", 32), new("GO", 52), new("MA", 21), new("MG", 31), new("MS", 50), new("MT", 51), new("PA", 15),
        new("PB", 25), new("PE", 26), new("PI", 22), new("PR", 41), new("RJ", 33), new("RN", 24), new("RO", 11),
        new("RR", 14), new("RS", 43), new("SC", 42), new("SE", 28), new("SP", 35), new("TO", 17),
    ];

    public static BrazilianState Parse(string? code) =>
        All.SingleOrDefault(s => s.Code.Equals(code?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new DomainException($"UF \"{code}\" inválida.");

    public override string ToString() => Code;
}

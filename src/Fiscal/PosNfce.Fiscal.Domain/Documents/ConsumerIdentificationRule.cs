using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Documents;

/// <summary>
/// From which total the NFC-e must identify the consumer. Each state sets its own value, so only verified states are
/// listed; for the others nothing is enforced here until the rule is checked.
/// </summary>
public static class ConsumerIdentificationRule
{
    /// <summary>
    /// MG: RICMS/2023, Anexo V, Parte 1, art. 29, VIII, "a", 1: identification required for "valor igual ou superior a
    /// R$10.000,00"; by CPF, CNPJ or foreigner's document (§ 2º).
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Money> MandatoryFromByState = new Dictionary<string, Money>
    {
        ["MG"] = new(10_000m),
    };

    public static Money? MandatoryFrom(BrazilianState state) =>
        MandatoryFromByState.TryGetValue(state.Code, out var value) ? value : null;

    public static void Check(Money total, string? customerCpf, BrazilianState state)
    {
        if (customerCpf is null && MandatoryFrom(state) is { } limit && total >= limit)
        {
            throw new DomainException($"NFC-e de {limit} ou mais precisa identificar o consumidor (CPF).");
        }
    }
}

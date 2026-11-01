using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;

namespace PosNfce.Fiscal.Application.Documents;

/// <summary>
/// Total from which the store's state requires the consumer's CPF on the NFC-e, or null when there is no such rule
/// (or no issuer yet). The checkout uses it to ask for the CPF before closing the sale.
/// </summary>
public sealed class GetConsumerIdentificationLimit(IIssuerRepository issuers)
{
    public async Task<decimal?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var issuer = await issuers.GetAsync(cancellationToken);
        return issuer is null ? null : ConsumerIdentificationRule.MandatoryFrom(issuer.Profile.Address.State)?.Amount;
    }
}

using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;

namespace PosNfce.Fiscal.Application.Documents;

/// <summary>
/// The NFC-e of a sale as the checkout shows it right after the sale.
/// <paramref name="WillAutoIssue"/>: pending and the issuer sends automatically, so <see cref="AutoIssueSaleNfce"/> will send it.
/// </summary>
public sealed record SaleNfce(FiscalDocumentStatus Status, int? Series, long? Number, string? LastMessage, bool WillAutoIssue);

/// <summary>Finds the NFC-e of a sale by the sale's id; null when no document was registered for it.</summary>
public sealed class GetSaleNfce(IFiscalDocumentRepository documents, IIssuerRepository issuers)
{
    public async Task<SaleNfce?> ExecuteAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        if (await documents.FindBySaleAsync(saleId, cancellationToken) is not { } d)
        {
            return null;
        }

        var autoIssue = d.Status == FiscalDocumentStatus.Pending && (await issuers.GetAsync(cancellationToken))?.AutoIssue == true;
        return new SaleNfce(d.Status, d.Series, d.Number, d.LastStatusCode is { } code ? $"{code} - {d.LastMessage}" : d.LastMessage, autoIssue);
    }
}

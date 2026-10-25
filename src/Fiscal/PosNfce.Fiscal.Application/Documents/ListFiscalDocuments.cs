using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;

namespace PosNfce.Fiscal.Application.Documents;

public sealed record FiscalDocumentSummary(
    Guid Id, DateTimeOffset SoldAt, int? Series, long? Number, int ItemCount, decimal Discount, decimal Total, FiscalDocumentStatus Status);

public sealed class ListFiscalDocuments(IFiscalDocumentRepository documents)
{
    public const int MaxResults = 200;

    public async Task<IReadOnlyList<FiscalDocumentSummary>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        (await documents.ListRecentAsync(MaxResults, cancellationToken))
            .Select(d => new FiscalDocumentSummary(d.Id, d.SoldAt, d.Series, d.Number, d.Items.Count, d.Discount.Amount, d.Total.Amount, d.Status))
            .ToList();
}

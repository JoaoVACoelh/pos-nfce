using PosNfce.Fiscal.Domain.Documents;

namespace PosNfce.Fiscal.Application.Ports;

public interface IFiscalDocumentRepository
{
    Task<bool> ExistsForSaleAsync(Guid saleId, CancellationToken cancellationToken = default);

    Task AddAsync(FiscalDocument document, CancellationToken cancellationToken = default);

    Task<FiscalDocument?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Saves the document's header (status, number, access key); items and payments never change.</summary>
    Task UpdateAsync(FiscalDocument document, CancellationToken cancellationToken = default);

    /// <summary>Whether any document already took a number: from then on the series and numbering are fixed.</summary>
    Task<bool> AnyNumberedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FiscalDocument>> ListRecentAsync(int limit, CancellationToken cancellationToken = default);
}

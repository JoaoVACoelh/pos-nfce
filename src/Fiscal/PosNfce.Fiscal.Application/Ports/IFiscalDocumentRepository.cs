using PosNfce.Fiscal.Domain.Documents;

namespace PosNfce.Fiscal.Application.Ports;

public interface IFiscalDocumentRepository
{
    Task<bool> ExistsForSaleAsync(Guid saleId, CancellationToken cancellationToken = default);

    Task AddAsync(FiscalDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FiscalDocument>> ListRecentAsync(int limit, CancellationToken cancellationToken = default);
}

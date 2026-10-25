using PosNfce.Catalog.Domain.TaxRules;

namespace PosNfce.Catalog.Application.Ports;

public interface ITaxRuleRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaxRule>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TaxRule taxRule, CancellationToken cancellationToken = default);
}

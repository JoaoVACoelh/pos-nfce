using PosNfce.Fiscal.Domain.Taxes;

namespace PosNfce.Fiscal.Application.Ports;

public interface IIbsCbsRateRepository
{
    /// <summary>The rates in force on <paramref name="date"/>: the most recent ones that started on or before it.</summary>
    Task<IbsCbsRates?> FindForDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<bool> ExistsFromAsync(DateOnly validFrom, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IbsCbsRates>> ListAsync(CancellationToken cancellationToken = default);

    Task AddAsync(IbsCbsRates rates, CancellationToken cancellationToken = default);
}

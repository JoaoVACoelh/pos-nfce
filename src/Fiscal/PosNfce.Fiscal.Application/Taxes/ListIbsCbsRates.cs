using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Taxes;

namespace PosNfce.Fiscal.Application.Taxes;

public sealed class ListIbsCbsRates(IIbsCbsRateRepository rates)
{
    public async Task<IReadOnlyList<IbsCbsRates>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        (await rates.ListAsync(cancellationToken)).OrderByDescending(r => r.ValidFrom).ToList();
}

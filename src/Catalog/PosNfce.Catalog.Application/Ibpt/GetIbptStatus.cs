using PosNfce.Catalog.Application.Ports;

namespace PosNfce.Catalog.Application.Ibpt;

public sealed record IbptStatus(string Version, DateOnly ValidFrom, DateOnly ValidUntil, int Count, bool Expired);

public sealed class GetIbptStatus(IIbptRateRepository rates, TimeProvider clock)
{
    public async Task<IbptStatus?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var summary = await rates.GetSummaryAsync(cancellationToken);
        if (summary is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        return new IbptStatus(summary.Version, summary.ValidFrom, summary.ValidUntil, summary.Count, today > summary.ValidUntil);
    }
}

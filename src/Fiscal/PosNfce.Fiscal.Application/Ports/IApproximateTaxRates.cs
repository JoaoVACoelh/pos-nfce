using PosNfce.Fiscal.Domain.Taxes;

namespace PosNfce.Fiscal.Application.Ports;

/// <summary>Approximate tax rates (IBPT) of a product. The adapter asks the Catalog module, which keeps the IBPT table.</summary>
public interface IApproximateTaxRates
{
    /// <summary>Null when the NCM isn't in the table (the receipt then shows no approximate taxes for the item).</summary>
    Task<ApproximateRatesWithSource?> FindAsync(string ncm, int origin, CancellationToken cancellationToken = default);
}

public sealed record ApproximateRatesWithSource(ApproximateRates Rates, string Source);

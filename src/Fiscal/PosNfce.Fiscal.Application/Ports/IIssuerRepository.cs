using PosNfce.Fiscal.Domain.Issuers;

namespace PosNfce.Fiscal.Application.Ports;

/// <summary>There is a single issuer per installation.</summary>
public interface IIssuerRepository
{
    Task<Issuer?> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(Issuer issuer, CancellationToken cancellationToken = default);
}

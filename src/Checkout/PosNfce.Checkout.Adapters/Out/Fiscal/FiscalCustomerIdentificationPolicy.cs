using PosNfce.Checkout.Application.Ports;
using PosNfce.Fiscal.Application.Documents;

namespace PosNfce.Checkout.Adapters.Out.Fiscal;

/// <summary>Implements Checkout's port with the Fiscal module's own use case: no access to Fiscal's tables.</summary>
public sealed class FiscalCustomerIdentificationPolicy(GetConsumerIdentificationLimit getLimit) : ICustomerIdentificationPolicy
{
    public Task<decimal?> CpfRequiredFromAsync(CancellationToken cancellationToken = default) => getLimit.ExecuteAsync(cancellationToken);
}

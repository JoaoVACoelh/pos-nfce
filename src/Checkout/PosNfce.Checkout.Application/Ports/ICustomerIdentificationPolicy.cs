namespace PosNfce.Checkout.Application.Ports;

/// <summary>
/// From which total the customer's CPF is required on the receipt. It's a fiscal rule (it depends on the store's
/// state), so the adapter asks the Fiscal module; Checkout only applies the value.
/// </summary>
public interface ICustomerIdentificationPolicy
{
    /// <summary>Null when no total requires it.</summary>
    Task<decimal?> CpfRequiredFromAsync(CancellationToken cancellationToken = default);
}

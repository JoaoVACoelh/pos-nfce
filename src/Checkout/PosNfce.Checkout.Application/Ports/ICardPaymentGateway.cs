using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Application.Ports;

/// <summary>
/// Charges a card or PIX payment. The first adapter is the standalone terminal: the cashier charges on the
/// terminal and the app just records it. A TEF adapter will talk to the terminal and return the authorization.
/// </summary>
public interface ICardPaymentGateway
{
    Task<CardCharge> ChargeAsync(PaymentMethod method, Money amount, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a charge. <paramref name="Integrated"/> tells the NFC-e whether the payment went through an
/// integrated system (TEF) or a standalone terminal (<c>tpIntegra</c>).
/// </summary>
public sealed record CardCharge(bool Integrated);

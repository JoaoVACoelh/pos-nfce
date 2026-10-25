using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Adapters.Out.Payments;

/// <summary>
/// Separate card terminal, not connected to the app: the cashier types the amount on the terminal and confirms
/// here. Nothing to call; the payment is recorded as not integrated.
/// </summary>
public sealed class StandaloneTerminalGateway : ICardPaymentGateway
{
    public Task<CardCharge> ChargeAsync(PaymentMethod method, Money amount, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CardCharge(Integrated: false));
}

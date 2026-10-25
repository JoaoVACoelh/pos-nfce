using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Application.Sales;

public sealed class AddSalePayment(ISaleRepository sales, ICardPaymentGateway cards)
{
    public async Task<SaleView> ExecuteAsync(PaymentMethod method, decimal amount, CancellationToken cancellationToken = default)
    {
        var sale = await OpenSale.LoadAsync(sales, cancellationToken);
        var money = new Money(amount);

        // Validate first: with TEF, a charge the sale would then refuse would leave money taken from the customer.
        sale.CheckPayment(method, money);
        var integrated = method != PaymentMethod.Cash && (await cards.ChargeAsync(method, money, cancellationToken)).Integrated;

        sale.AddPayment(method, money, integrated);
        await sales.SaveAsync(sale, cancellationToken);
        return SaleView.From(sale);
    }
}

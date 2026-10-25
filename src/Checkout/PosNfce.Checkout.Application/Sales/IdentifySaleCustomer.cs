using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Domain.Sales;

namespace PosNfce.Checkout.Application.Sales;

/// <summary>"CPF na nota": sets the customer's CPF on the open sale; empty text removes it.</summary>
public sealed class IdentifySaleCustomer(ISaleRepository sales)
{
    public async Task<SaleView> ExecuteAsync(string? cpf, CancellationToken cancellationToken = default)
    {
        var sale = await OpenSale.LoadAsync(sales, cancellationToken);
        sale.IdentifyCustomer(string.IsNullOrWhiteSpace(cpf) ? null : Cpf.Parse(cpf));
        await sales.SaveAsync(sale, cancellationToken);
        return SaleView.From(sale);
    }
}

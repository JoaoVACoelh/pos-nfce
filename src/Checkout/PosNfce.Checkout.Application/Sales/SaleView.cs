using PosNfce.Checkout.Domain.Sales;

namespace PosNfce.Checkout.Application.Sales;

/// <summary>The sale as the checkout screen shows it. Every use case returns it, so the screen just redraws.</summary>
public sealed record SaleView(
    Guid? Id,
    IReadOnlyList<SaleItemView> Items,
    IReadOnlyList<PaymentView> Payments,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    decimal Paid,
    decimal Remaining,
    decimal Change,
    string? CustomerCpf = null)
{
    public static SaleView Empty { get; } = new(null, [], [], 0, 0, 0, 0, 0, 0);

    public static SaleView From(Sale? sale) => sale is null || sale.Status != SaleStatus.Open
        ? Empty
        : new SaleView(
            sale.Id,
            sale.Items.Select(i => new SaleItemView(i.Id, i.Line, i.Code, i.Description, i.Unit, i.Quantity, i.UnitPrice.Amount, i.Total.Amount)).ToList(),
            sale.Payments.Select(p => new PaymentView(p.Id, p.Method, p.Amount.Amount)).ToList(),
            sale.Subtotal.Amount, sale.Discount.Amount, sale.Total.Amount, sale.Paid.Amount, sale.Remaining.Amount, sale.Change.Amount,
            sale.CustomerCpf?.Formatted);
}

public sealed record SaleItemView(Guid Id, int Line, string Code, string Description, string Unit, decimal Quantity, decimal UnitPrice, decimal Total);

public sealed record PaymentView(Guid Id, PaymentMethod Method, decimal Amount);

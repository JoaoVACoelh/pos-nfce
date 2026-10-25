using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Domain.Sales;

/// <summary>One line of the sale, with the price and tax data copied from the product when it was scanned.</summary>
public sealed class SaleItem
{
    /// <summary>The NF-e layout allows up to 4 decimal places in the quantity (<c>qCom</c>).</summary>
    public const int QuantityDecimals = 4;

    public Guid Id { get; }
    public int Line { get; }
    public Guid ProductId { get; }
    public string Code { get; }
    public string Gtin { get; }
    public string Description { get; }
    public string Unit { get; }
    public Money UnitPrice { get; }
    public decimal Quantity { get; }
    public TaxSnapshot Tax { get; }

    /// <summary>Quantity × unit price rounded to cents, half away from zero (commercial rounding).</summary>
    public Money Total => new(decimal.Round(UnitPrice.Amount * Quantity, 2, MidpointRounding.AwayFromZero));

    public SaleItem(
        Guid id, int line, Guid productId, string code, string gtin, string description, string unit,
        Money unitPrice, decimal quantity, TaxSnapshot tax)
    {
        if (quantity <= 0)
        {
            throw new DomainException("A quantidade deve ser maior que zero.");
        }

        if (decimal.Round(quantity, QuantityDecimals) != quantity)
        {
            throw new DomainException($"A quantidade pode ter no máximo {QuantityDecimals} casas decimais.");
        }

        if (unitPrice <= Money.Zero)
        {
            throw new DomainException($"O produto \"{description}\" está sem preço.");
        }

        Id = id;
        Line = line;
        ProductId = productId;
        Code = code;
        Gtin = gtin;
        Description = description;
        Unit = unit;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Tax = tax;
    }
}

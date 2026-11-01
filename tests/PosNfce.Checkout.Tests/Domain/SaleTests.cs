using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Tests.Domain;

public class SaleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 15, 0, 0, TimeSpan.FromHours(-3));

    /// <summary>Format-only tax values: real ones come from the accountant.</summary>
    internal static readonly TaxSnapshot Tax = new("00000000", null, 0, "5102", "102", "49", 0, "49", 0, "000", "000001", 0m);

    private static Sale SaleWith(params (decimal price, decimal quantity)[] items)
    {
        var sale = Sale.Open(Now);
        foreach (var (price, quantity) in items)
        {
            sale.AddItem(Guid.NewGuid(), "001", "SEM GTIN", "Produto", "KG", new Money(price), quantity, Tax);
        }

        return sale;
    }

    [Fact]
    public void Item_total_rounds_to_cents_half_away_from_zero()
    {
        // 0,350 kg × 12,90 = 4,515 → 4,52
        var sale = SaleWith((12.90m, 0.350m));

        Assert.Equal(new Money(4.52m), sale.Items[0].Total);
    }

    [Fact]
    public void Cash_change_has_a_limit()
    {
        var sale = SaleWith((10m, 1));

        Assert.Throws<DomainException>(() => sale.AddPayment(PaymentMethod.Cash, new Money(1_010.01m)));

        sale.AddPayment(PaymentMethod.Cash, new Money(1_010m));
        Assert.Equal(new Money(1_000m), sale.Change);
    }

    [Fact]
    public void Totals_add_items_and_subtract_the_discount()
    {
        var sale = SaleWith((10m, 2), (5.50m, 1));
        sale.ApplyDiscount(new Money(1.50m));

        Assert.Equal((25.50m, 1.50m, 24.00m), (sale.Subtotal.Amount, sale.Discount.Amount, sale.Total.Amount));
    }

    [Fact]
    public void Lines_are_numbered_in_order()
    {
        var sale = SaleWith((1m, 1), (2m, 1), (3m, 1));

        Assert.Equal([1, 2, 3], sale.Items.Select(i => i.Line));
    }

    [Fact]
    public void Discount_must_leave_something_to_pay()
    {
        var sale = SaleWith((10m, 1));

        Assert.Throws<DomainException>(() => sale.ApplyDiscount(new Money(10m)));
    }

    [Fact]
    public void Cash_can_exceed_the_total_and_gives_change()
    {
        var sale = SaleWith((17.30m, 1));

        sale.AddPayment(PaymentMethod.Cash, new Money(20m));

        Assert.Equal((Money.Zero, new Money(2.70m)), (sale.Remaining, sale.Change));
    }

    [Fact]
    public void Card_cannot_exceed_what_is_left()
    {
        var sale = SaleWith((17.30m, 1));

        Assert.Throws<DomainException>(() => sale.AddPayment(PaymentMethod.CreditCard, new Money(20m)));
    }

    [Fact]
    public void Split_payment_card_then_cash()
    {
        var sale = SaleWith((50m, 1));

        sale.AddPayment(PaymentMethod.DebitCard, new Money(30m));
        sale.AddPayment(PaymentMethod.Cash, new Money(50m));

        Assert.Equal(new Money(30m), sale.Change);
    }

    [Fact]
    public void Sale_is_completed_only_when_fully_paid()
    {
        var sale = SaleWith((10m, 1));
        sale.AddPayment(PaymentMethod.Pix, new Money(4m));

        Assert.Throws<DomainException>(() => sale.Complete(Now));

        sale.AddPayment(PaymentMethod.Pix, new Money(6m));
        sale.Complete(Now);
        Assert.Equal(SaleStatus.Completed, sale.Status);
    }

    [Fact]
    public void Items_cannot_change_after_a_payment()
    {
        var sale = SaleWith((10m, 1));
        sale.AddPayment(PaymentMethod.Cash, new Money(5m));

        Assert.Throws<DomainException>(() => sale.RemoveItem(sale.Items[0].Id));
    }

    [Fact]
    public void Removing_items_drops_a_discount_that_no_longer_fits()
    {
        var sale = SaleWith((10m, 1), (2m, 1));
        sale.ApplyDiscount(new Money(5m));

        sale.RemoveItem(sale.Items[0].Id);

        Assert.Equal(Money.Zero, sale.Discount);
    }

    [Fact]
    public void Product_sold_by_unit_refuses_a_fraction()
    {
        var sale = Sale.Open(Now);

        var error = Assert.Throws<DomainException>(() => sale.AddItem(Guid.NewGuid(), "1", "SEM GTIN", "Refrigerante", "UN", new Money(5m), 0.5m, Tax));

        Assert.Contains("por unidade", error.Message);
        Assert.Empty(sale.Items);
    }

    [Fact]
    public void Product_sold_by_unit_accepts_whole_quantities_written_with_decimals()
    {
        var sale = Sale.Open(Now);

        sale.AddItem(Guid.NewGuid(), "1", "SEM GTIN", "Refrigerante", "un", new Money(5m), 2.000m, Tax);

        Assert.Equal(new Money(10m), sale.Total);
    }

    [Fact]
    public void Item_that_rounds_to_zero_is_refused()
    {
        var sale = Sale.Open(Now);

        // 0,0001 kg × 21,90 = 0,00219 → R$ 0,00
        var error = Assert.Throws<DomainException>(() => sale.AddItem(Guid.NewGuid(), "1", "SEM GTIN", "Pão", "KG", new Money(21.90m), 0.0001m, Tax));

        Assert.Contains("pequena demais", error.Message);
        Assert.Empty(sale.Items);
    }

    [Fact]
    public void Saved_sale_with_a_fraction_of_a_unit_still_loads()
    {
        var item = new SaleItem(Guid.NewGuid(), 1, Guid.NewGuid(), "1", "SEM GTIN", "Refrigerante", "UN", new Money(5m), 0.5m, Tax);

        var sale = Sale.Restore(Guid.NewGuid(), SaleStatus.Open, Now, null, Money.Zero, [item], []);

        Assert.Equal(new Money(2.50m), sale.Total);
    }

    [Fact]
    public void Closed_sale_cannot_change()
    {
        var sale = SaleWith((10m, 1));
        sale.Cancel(Now);

        Assert.Throws<DomainException>(() => sale.AddItem(Guid.NewGuid(), "1", "SEM GTIN", "X", "UN", new Money(1m), 1, Tax));
    }

    [Theory]
    [InlineData("7891234567895", 1, "7891234567895")]
    [InlineData("3*7891234567895", 3, "7891234567895")]
    [InlineData("0,350*PAO", 0.350, "PAO")]
    [InlineData("1.5 * PAO", 1.5, "PAO")]
    public void Scan_input_reads_optional_quantity(string typed, decimal quantity, string code)
    {
        var input = ScanInput.Parse(typed);

        Assert.Equal((quantity, code), (input.Quantity, input.Code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0*PAO")]
    [InlineData("abc*PAO")]
    [InlineData("3*")]
    public void Scan_input_rejects_invalid_text(string typed)
    {
        Assert.Throws<DomainException>(() => ScanInput.Parse(typed));
    }
}

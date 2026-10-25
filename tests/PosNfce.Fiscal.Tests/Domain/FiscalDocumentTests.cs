using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Domain;

public class FiscalDocumentTests
{
    private static IReadOnlyList<Money> Totals(params decimal[] values) => values.Select(v => new Money(v)).ToList();

    [Fact]
    public void Discount_is_split_in_proportion_to_each_item()
    {
        var shares = DiscountApportionment.Split(new Money(3m), Totals(10m, 20m));

        Assert.Equal([1m, 2m], shares.Select(s => s.Amount));
    }

    [Fact]
    public void Leftover_cents_go_to_the_largest_items_and_the_sum_matches()
    {
        // 1,00 over three equal items: 0,33 each and the extra cent to the first of the largest.
        var shares = DiscountApportionment.Split(new Money(1m), Totals(5m, 5m, 5m));

        Assert.Equal([0.34m, 0.33m, 0.33m], shares.Select(s => s.Amount));
    }

    [Fact]
    public void Small_items_never_get_more_discount_than_their_total()
    {
        var totals = Totals(0.01m, 0.01m, 100m);

        var shares = DiscountApportionment.Split(new Money(99.99m), totals);

        Assert.Equal(99.99m, shares.Sum(s => s.Amount));
        Assert.All(shares.Zip(totals), pair => Assert.True(pair.First <= pair.Second));
    }

    [Fact]
    public void No_discount_means_zero_for_every_item()
    {
        Assert.All(DiscountApportionment.Split(Money.Zero, Totals(1m, 2m)), s => Assert.Equal(Money.Zero, s));
    }

    [Fact]
    public void Document_from_sale_gets_item_discounts_and_starts_pending_without_number()
    {
        var document = FiscalDocument.FromSale(
            Guid.NewGuid(), DateTimeOffset.Now,
            [Item(1, 10m), Item(2, 20m)], new Money(3m), new Money(27m), new Money(3m),
            [new FiscalPayment("Cash", new Money(30m), false)]);

        Assert.Equal(FiscalDocumentStatus.Pending, document.Status);
        Assert.Null(document.Number);
        Assert.Equal([9m, 18m], document.Items.Select(i => i.NetTotal.Amount));
    }

    [Fact]
    public void Sale_total_that_does_not_match_the_items_is_rejected()
    {
        Assert.Throws<DomainException>(() => FiscalDocument.FromSale(
            Guid.NewGuid(), DateTimeOffset.Now, [Item(1, 10m)], Money.Zero, new Money(9m), Money.Zero,
            [new FiscalPayment("Cash", new Money(9m), false)]));
    }

    [Fact]
    public void Number_is_assigned_only_once()
    {
        var document = FiscalDocument.FromSale(
            Guid.NewGuid(), DateTimeOffset.Now, [Item(1, 10m)], Money.Zero, new Money(10m), Money.Zero,
            [new FiscalPayment("Pix", new Money(10m), false)]);

        document.AssignNumber(1, 7);

        Assert.Throws<DomainException>(() => document.AssignNumber(1, 8));
    }

    internal static FiscalItem Item(int line, decimal total) => new(
        line, $"P{line}", "SEM GTIN", "Produto", "UN", 1m, new Money(total), new Money(total), Money.Zero,
        "00000000", null, 0, "5102", "102", "49", 0m, "49", 0m, "000", "000001", 0m);
}

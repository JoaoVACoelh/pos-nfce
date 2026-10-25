using PosNfce.SharedKernel;

namespace PosNfce.SharedKernel.Tests;

public class MoneyTests
{
    [Fact]
    public void Adding_amounts_keeps_decimal_precision()
    {
        // 0.1 + 0.2 is 0.30000000000000004 with double; decimal must be exact.
        var total = new Money(0.1m) + new Money(0.2m);

        Assert.Equal(new Money(0.3m), total);
    }

    [Fact]
    public void Multiplying_by_a_fractional_quantity_does_not_round()
    {
        var total = new Money(9.99m) * 1.235m;

        Assert.Equal(12.33765m, total.Amount);
    }

    [Fact]
    public void Subtracting_more_than_the_amount_gives_a_negative_value()
    {
        var change = new Money(10m) - new Money(12.5m);

        Assert.True(change.IsNegative);
    }

    [Fact]
    public void Formats_in_brazilian_style_whatever_the_machine_culture()
    {
        Assert.Equal("R$ 1.234,50", new Money(1234.5m).ToString());
    }
}

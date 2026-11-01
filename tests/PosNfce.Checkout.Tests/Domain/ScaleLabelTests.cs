using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Tests.Domain;

public class ScaleLabelTests
{
    [Fact]
    public void Client_label_gives_the_item_code_and_the_weight()
    {
        // Real label from the first client's scale: PAO FRANCES, 0,364 kg × R$ 21,90 = R$ 7,97.
        Assert.True(ScaleLabel.TryParse("2000005003646", out var label));

        Assert.Equal(new ScaleLabel(5, 0.364m), label);
    }

    [Fact]
    public void Code_with_more_digits_is_read_whole()
    {
        Assert.True(ScaleLabel.TryParse("2001352001903", out var label));

        Assert.Equal(new ScaleLabel(1352, 0.190m), label);
    }

    [Theory]
    [InlineData("2000005003647")] // wrong check digit
    [InlineData("7891234567895")] // ordinary GTIN
    [InlineData("200000500364")] // 12 digits
    [InlineData("PAO")]
    [InlineData("2000005OO3646")]
    public void Anything_else_is_not_a_scale_label(string code)
    {
        Assert.False(ScaleLabel.TryParse(code, out _));
    }

    [Fact]
    public void Weight_is_the_quantity_of_a_product_sold_by_kg()
    {
        Assert.Equal(0.364m, new ScaleLabel(5, 0.364m).QuantityFor("kg", "Pão francês"));
    }

    [Fact]
    public void Product_sold_by_unit_is_refused()
    {
        var error = Assert.Throws<DomainException>(() => new ScaleLabel(5, 0.364m).QuantityFor("UN", "Refrigerante"));

        Assert.Contains("Refrigerante", error.Message);
    }
}

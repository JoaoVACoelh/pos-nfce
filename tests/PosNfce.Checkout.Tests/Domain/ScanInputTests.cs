using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Tests.Domain;

public class ScanInputTests
{
    [Fact]
    public void Code_alone_of_a_product_sold_by_kg_asks_for_the_weight()
    {
        var error = Assert.Throws<WeightRequiredException>(() => ScanInput.Parse("000005").QuantityFor("KG", "Pão francês"));

        Assert.Contains("Pão francês", error.Message);
    }

    [Fact]
    public void Typed_weight_is_used()
    {
        Assert.Equal(0.350m, ScanInput.Parse("0,350*000005").QuantityFor("KG", "Pão francês"));
    }

    [Fact]
    public void Code_alone_of_a_product_sold_by_unit_is_one_unit()
    {
        Assert.Equal(1m, ScanInput.Parse("7891234567895").QuantityFor("UN", "Refrigerante"));
    }

    [Fact]
    public void Barcode_read_in_the_quantity_field_is_refused()
    {
        Assert.Throws<DomainException>(() => ScanInput.Parse("2000005003646*000005"));
    }
}

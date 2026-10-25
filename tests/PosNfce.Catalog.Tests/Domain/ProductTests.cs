using PosNfce.Catalog.Domain.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Tests.Domain;

public class ProductTests
{
    private static Product Register(
        string code = "001", string description = "Produto de teste", string unit = "un",
        decimal price = 9.90m, ProductOrigin origin = ProductOrigin.Domestic, Guid? taxRuleId = null) =>
        Product.Register(
            code, Gtin.None, description, Ncm.Parse("00000000"), null, unit,
            new Money(price), origin, taxRuleId ?? ProductSamples.TaxRuleId);

    [Fact]
    public void New_product_is_active_and_unit_is_upper_case()
    {
        var product = Register(unit: "kg");

        Assert.True(product.Active);
        Assert.Equal("KG", product.Unit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Price_must_be_positive(decimal price)
    {
        Assert.Throws<DomainException>(() => Register(price: price));
    }

    [Fact]
    public void Description_longer_than_the_layout_limit_is_rejected()
    {
        Assert.Throws<DomainException>(() => Register(description: new string('x', Product.DescriptionMaxLength + 1)));
    }

    [Fact]
    public void Code_is_required()
    {
        Assert.Throws<DomainException>(() => Register(code: "  "));
    }

    [Fact]
    public void Tax_rule_is_required()
    {
        Assert.Throws<DomainException>(() => Register(taxRuleId: Guid.Empty));
    }

    [Fact]
    public void Origin_outside_the_layout_table_is_rejected()
    {
        Assert.Throws<DomainException>(() => Register(origin: (ProductOrigin)9));
    }
}

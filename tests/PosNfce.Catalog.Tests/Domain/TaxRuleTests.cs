using PosNfce.Catalog.Domain.TaxRules;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Tests.Domain;

public class TaxRuleTests
{
    [Theory]
    [InlineData("5102", "5102")]
    [InlineData("5.405", "5405")]
    public void Cfop_accepts_intrastate_codes_with_or_without_dot(string input, string expected)
    {
        Assert.Equal(expected, Cfop.Parse(input).Value);
    }

    [Theory]
    [InlineData("6102")] // interstate: not allowed in NFC-e
    [InlineData("1102")] // purchase
    [InlineData("510")]
    [InlineData("51O2")]
    public void Cfop_rejects_codes_outside_nfce(string input)
    {
        Assert.Throws<DomainException>(() => Cfop.Parse(input));
    }

    [Theory]
    [InlineData("102")]
    [InlineData("500")]
    [InlineData("900")]
    public void Csosn_accepts_layout_codes(string input)
    {
        Assert.Equal(input, Csosn.Parse(input).Value);
    }

    [Theory]
    [InlineData("100")]
    [InlineData("00")] // CST of the normal regime, not CSOSN
    [InlineData("")]
    public void Csosn_rejects_other_values(string input)
    {
        Assert.Throws<DomainException>(() => Csosn.Parse(input));
    }

    [Theory]
    [InlineData("01")]
    [InlineData("49")]
    [InlineData("99")]
    public void Pis_cofins_cst_accepts_layout_codes(string input)
    {
        Assert.Equal(input, PisCofinsCst.Parse(input, "PIS").Value);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("10")]
    [InlineData("76")]
    public void Pis_cofins_cst_rejects_other_values(string input)
    {
        Assert.Throws<DomainException>(() => PisCofinsCst.Parse(input, "PIS"));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [InlineData(1.23456)]
    public void Rate_must_be_between_0_and_100_with_up_to_four_decimals(decimal value)
    {
        Assert.Throws<DomainException>(() => Percentage.Of(value, "PIS"));
    }

    [Theory]
    [InlineData("00", "000001")]
    [InlineData("000", "00001")]
    [InlineData("000", "00000A")]
    public void Tax_reform_codes_check_the_format(string cst, string classTrib)
    {
        Assert.Throws<DomainException>(() => TaxReformCodes.Parse(cst, classTrib));
    }

    [Fact]
    public void Name_is_required()
    {
        Assert.Throws<DomainException>(() => TaxRule.Register(
            " ", Cfop.Parse("5102"), Csosn.Parse("102"),
            PisCofinsCst.Parse("49", "PIS"), Percentage.Zero, PisCofinsCst.Parse("49", "COFINS"), Percentage.Zero,
            TaxReformCodes.Parse("000", "000001")));
    }
}

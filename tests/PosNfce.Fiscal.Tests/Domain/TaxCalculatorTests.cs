using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.Fiscal.Domain.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Domain;

/// <summary>Format-only tax values: the real ones come from the accountant.</summary>
public class TaxCalculatorTests
{
    private static readonly DateOnly In2026 = new(2026, 10, 25);

    private static FiscalItem Item(string csosn = "102", string pisCst = "49", decimal pisRate = 0m, decimal total = 10m, decimal discount = 0m) =>
        FiscalDocumentTests.Item(1, total) with
        {
            Csosn = csosn, PisCst = pisCst, PisRate = pisRate, CofinsCst = pisCst, CofinsRate = pisRate * 4.6m, Discount = new Money(discount),
        };

    private static ItemTaxes Calculate(FiscalItem item, ApproximateRates? rates = null, DateOnly? date = null) =>
        TaxCalculator.Default.Calculate(TaxRegime.SimplesNacional, item, rates, date ?? In2026);

    [Theory]
    [InlineData("102")]
    [InlineData("103")]
    [InlineData("300")]
    [InlineData("400")]
    public void Csosn_without_credit_or_st_goes_in_the_sn102_group(string csosn)
    {
        var icms = Calculate(Item(csosn)).Icms;

        Assert.Equal((SimplesIcmsGroup.Sn102, csosn, 0), (icms.Group, icms.Csosn, icms.Origin));
    }

    [Fact]
    public void Csosn_500_goes_in_the_sn500_group()
    {
        Assert.Equal(SimplesIcmsGroup.Sn500, Calculate(Item("500")).Icms.Group);
    }

    [Theory]
    [InlineData("101")]
    [InlineData("201")]
    [InlineData("900")]
    public void Csosn_with_credit_or_st_calculation_is_refused(string csosn)
    {
        Assert.Throws<DomainException>(() => Calculate(Item(csosn)));
    }

    [Fact]
    public void Pis_cofins_49_with_zero_rate_has_zero_base_and_value()
    {
        var taxes = Calculate(Item(pisCst: "49"));

        Assert.Equal((PisCofinsGroup.Other, Money.Zero, Money.Zero), (taxes.Pis.Group, taxes.Pis.Base, taxes.Pis.Value));
    }

    [Fact]
    public void Monophasic_cst_04_only_carries_the_cst()
    {
        var taxes = Calculate(Item(pisCst: "04"));

        Assert.Equal((PisCofinsGroup.NotTaxed, "04", Money.Zero), (taxes.Cofins.Group, taxes.Cofins.Cst, taxes.Cofins.Value));
    }

    [Fact]
    public void Rate_cst_uses_the_net_value_as_base_and_rounds_to_cents()
    {
        // Net 9,00 (10,00 - 1,00 discount) × 1,65% = 0,1485 → 0,15
        var taxes = Calculate(Item(pisCst: "01", pisRate: 1.65m, total: 10m, discount: 1m));

        Assert.Equal((PisCofinsGroup.Rate, 9m, 0.15m), (taxes.Pis.Group, taxes.Pis.Base.Amount, taxes.Pis.Value.Amount));
    }

    [Fact]
    public void Approximate_tax_adds_the_three_ibpt_rates_over_the_net_value()
    {
        // 9,00 × (13,45 + 18,00 + 0) % = 2,8305 → 2,83
        var taxes = Calculate(Item(total: 10m, discount: 1m), new ApproximateRates(13.45m, 18m, 0m));

        Assert.Equal(new Money(2.83m), taxes.ApproximateTax);
    }

    [Fact]
    public void Without_ibpt_rates_the_approximate_tax_is_zero()
    {
        Assert.Equal(Money.Zero, Calculate(Item()).ApproximateTax);
    }

    [Fact]
    public void From_2027_simples_nacional_needs_ibs_cbs_and_the_calculator_refuses_until_updated()
    {
        var error = Assert.Throws<DomainException>(() => Calculate(Item(), date: SimplesNacionalTaxStrategy.IbsCbsStart));
        Assert.Contains("IBS/CBS", error.Message);
    }
}

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

    private static readonly DateOnly In2027 = SimplesNacionalTaxStrategy.IbsCbsStart;

    /// <summary>Fictitious rates for the tests; the real ones are registered from the law / the accountant.</summary>
    private static readonly IbsCbsRates TestRates = new(In2027, ibsState: 0.5m, ibsMunicipal: 0.5m, cbs: 9m, "TESTE");

    private static ItemTaxes Calculate(FiscalItem item, ApproximateRates? rates = null, DateOnly? date = null, IbsCbsRates? ibsCbs = null) =>
        TaxCalculator.Default.Calculate(TaxRegime.SimplesNacional, item, rates, ibsCbs, date ?? In2026);

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
    public void In_2026_simples_nacional_does_not_fill_ibs_cbs()
    {
        Assert.Null(Calculate(Item(), ibsCbs: TestRates).IbsCbs);
    }

    [Fact]
    public void From_2027_without_registered_rates_issuing_is_refused()
    {
        var error = Assert.Throws<DomainException>(() => Calculate(Item(), date: In2027));
        Assert.Contains("alíquotas de IBS/CBS", error.Message);
    }

    [Fact]
    public void From_2027_full_taxation_applies_the_rates_to_the_net_value()
    {
        // Net 9,00; IBS 0,5% + 0,5% = 0,05 + 0,05; CBS 9% = 0,81
        var ibsCbs = Calculate(Item(total: 10m, discount: 1m), date: In2027, ibsCbs: TestRates).IbsCbs!;

        Assert.True(ibsCbs.Taxed);
        Assert.Equal(9m, ibsCbs.Base.Amount);
        Assert.Equal((0.05m, 0.05m, 0.10m, 0.81m), (ibsCbs.IbsState.Amount, ibsCbs.IbsMunicipal.Amount, ibsCbs.Ibs.Amount, ibsCbs.Cbs.Amount));
    }

    [Fact]
    public void Pis_and_cofins_values_stay_out_of_the_ibs_cbs_base()
    {
        // Net 100,00; PIS 1,65 and COFINS 7,59 (CST 01) → base 90,76
        var item = Item(pisCst: "01", pisRate: 1.65m, total: 100m);

        var ibsCbs = Calculate(item, date: In2027, ibsCbs: TestRates).IbsCbs!;

        Assert.Equal(90.76m, ibsCbs.Base.Amount);
    }

    [Fact]
    public void Reduced_rate_uses_the_reduction_from_the_tax_rule()
    {
        // CBS 9% with 60% reduction → 3,6% effective; net 10,00 → 0,36
        var item = Item() with { IbsCbsCst = "200", IbsCbsReduction = 60m };

        var ibsCbs = Calculate(item, date: In2027, ibsCbs: TestRates).IbsCbs!;

        Assert.Equal((3.6m, 0.36m, 60m), (ibsCbs.CbsRate, ibsCbs.Cbs.Amount, ibsCbs.Reduction));
    }

    [Theory]
    [InlineData("400")]
    [InlineData("410")]
    public void Exemption_and_immunity_carry_only_cst_and_classification(string cst)
    {
        var ibsCbs = Calculate(Item() with { IbsCbsCst = cst }, date: In2027, ibsCbs: TestRates).IbsCbs!;

        Assert.False(ibsCbs.Taxed);
        Assert.Equal((cst, "000001", Money.Zero), (ibsCbs.Cst, ibsCbs.ClassTrib, ibsCbs.Cbs));
    }

    [Theory]
    [InlineData("000", 10)]  // full taxation can't have a reduction
    [InlineData("200", 0)]   // reduced rate needs one
    [InlineData("510", 0)]   // deferral: not supported yet
    public void Inconsistent_or_unsupported_ibs_cbs_situations_are_refused(string cst, decimal reduction)
    {
        Assert.Throws<DomainException>(() =>
            Calculate(Item() with { IbsCbsCst = cst, IbsCbsReduction = reduction }, date: In2027, ibsCbs: TestRates));
    }

    [Fact]
    public void Ibs_cbs_rates_need_a_source_and_valid_percentages()
    {
        Assert.Throws<DomainException>(() => new IbsCbsRates(In2027, 0.1m, 0m, 0.9m, " "));
        Assert.Throws<DomainException>(() => new IbsCbsRates(In2027, 101m, 0m, 0.9m, "LC 214/2025"));
    }
}

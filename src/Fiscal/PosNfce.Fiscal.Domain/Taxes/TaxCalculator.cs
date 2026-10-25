using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Taxes;

/// <summary>One way of taxing items, per tax regime (CRT).</summary>
public interface ITaxStrategy
{
    TaxRegime Regime { get; }

    /// <param name="ibsCbsRates">Rates in force on the issue date; null when none was registered.</param>
    ItemTaxes Calculate(FiscalItem item, ApproximateRates? approximateRates, IbsCbsRates? ibsCbsRates, DateOnly issueDate);
}

/// <summary>Picks the strategy of the issuer's regime. Only Simples Nacional exists for now.</summary>
public sealed class TaxCalculator(IEnumerable<ITaxStrategy> strategies)
{
    private readonly Dictionary<TaxRegime, ITaxStrategy> _strategies = strategies.ToDictionary(s => s.Regime);

    public static TaxCalculator Default { get; } = new([new SimplesNacionalTaxStrategy()]);

    public ItemTaxes Calculate(
        TaxRegime regime, FiscalItem item, ApproximateRates? approximateRates, IbsCbsRates? ibsCbsRates, DateOnly issueDate) =>
        _strategies.TryGetValue(regime, out var strategy)
            ? strategy.Calculate(item, approximateRates, ibsCbsRates, issueDate)
            : throw new DomainException($"Regime tributário {regime} ainda não tem cálculo de impostos.");

    /// <summary>Money rounded to cents, half away from zero, as every value of the NF-e.</summary>
    internal static Money Cents(decimal value) => new(decimal.Round(value, 2, MidpointRounding.AwayFromZero));
}

/// <summary>
/// Simples Nacional (CRT 1). Rules checked on 2026-10-25 (see ROADMAP decision log):
/// - ICMS: CSOSN 102, 103, 300 and 400 go in ICMSSN102 with origin and CSOSN only. CSOSN 500 goes in ICMSSN500; its
///   "ICMS ST retido" fields (rejection 938) are required only when indFinal = 0, and the NFC-e is always to a final
///   consumer. The "ICMS efetivo" fields (rejection 906) are optional per state: not filled until SEFAZ-MG asks for them.
///   CSOSN 101, 201, 202, 203 and 900 need credit or ST calculations that aren't implemented: refused here.
/// - PIS/COFINS: group chosen by the CST from the layout table; value = base × rate, both from the tax rule.
/// - IBS/CBS: for CRT 1 they apply from 2027 (art. 348 LC 214/2025; NT 2025.002-RTC v1.40 sets production on 04/01/2027).
///   Before that date the group is not filled. From it, the item gets the group calculated by <see cref="IbsCbsCalculation"/>
///   with the rates registered for the date. The specific CRT 1 rules are still to come "in a future NT": review then.
/// - Approximate taxes (Lei 12.741/2012): net item value × (federal + state + municipal) IBPT rates.
/// </summary>
public sealed class SimplesNacionalTaxStrategy : ITaxStrategy
{
    public static readonly DateOnly IbsCbsStart = new(2027, 1, 4);

    private static readonly string[] Sn102Codes = ["102", "103", "300", "400"];

    public TaxRegime Regime => TaxRegime.SimplesNacional;

    public ItemTaxes Calculate(FiscalItem item, ApproximateRates? approximateRates, IbsCbsRates? ibsCbsRates, DateOnly issueDate)
    {
        var net = item.NetTotal;
        var pis = PisOrCofins(item.PisCst, item.PisRate, net, "PIS");
        var cofins = PisOrCofins(item.CofinsCst, item.CofinsRate, net, "COFINS");

        IbsCbsTaxes? ibsCbs = null;
        if (issueDate >= IbsCbsStart)
        {
            var rates = ibsCbsRates ?? throw new DomainException(
                $"Cadastre as alíquotas de IBS/CBS vigentes em {issueDate:dd/MM/yyyy}: o Simples Nacional informa IBS/CBS desde 04/01/2027.");
            ibsCbs = IbsCbsCalculation.Calculate(item, pis, cofins, rates);
        }

        return new ItemTaxes(
            Icms(item),
            pis,
            cofins,
            approximateRates is null ? Money.Zero : TaxCalculator.Cents(net.Amount * approximateRates.Total / 100m),
            ibsCbs);
    }

    private static SimplesIcms Icms(FiscalItem item)
    {
        if (Sn102Codes.Contains(item.Csosn))
        {
            return new SimplesIcms(SimplesIcmsGroup.Sn102, item.Csosn, item.Origin);
        }

        if (item.Csosn == "500")
        {
            return new SimplesIcms(SimplesIcmsGroup.Sn500, item.Csosn, item.Origin);
        }

        throw new DomainException($"CSOSN {item.Csosn} (item {item.Line}) ainda não é suportado na emissão da NFC-e.");
    }

    private static PisCofins PisOrCofins(string cst, decimal rate, Money net, string tax)
    {
        var code = int.Parse(cst);
        switch (code)
        {
            case 1 or 2:
                return new PisCofins(PisCofinsGroup.Rate, cst, net, rate, TaxCalculator.Cents(net.Amount * rate / 100m));
            case >= 4 and <= 9:
                return new PisCofins(PisCofinsGroup.NotTaxed, cst, Money.Zero, 0m, Money.Zero);
            case >= 49 and <= 99:
                // Usual for Simples Nacional: CST 49/99 with rate 0, which gives base and value 0.
                var taxBase = rate == 0m ? Money.Zero : net;
                return new PisCofins(PisCofinsGroup.Other, cst, taxBase, rate, TaxCalculator.Cents(taxBase.Amount * rate / 100m));
            default:
                // CST 03 is per quantity (rate in R$ per unit), not modeled in the tax rule.
                throw new DomainException($"CST {cst} de {tax} ainda não é suportado na emissão da NFC-e.");
        }
    }
}

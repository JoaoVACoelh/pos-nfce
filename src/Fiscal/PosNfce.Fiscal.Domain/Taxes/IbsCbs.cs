using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Taxes;

/// <summary>
/// IBS (state and municipal parts) and CBS rates in force from a date, in percent. They change by law during the
/// transition (2026 test year, then yearly until 2033), so they are data typed in by the operator from the accountant's
/// information, with the legal source, never constants in the code.
/// </summary>
public sealed record IbsCbsRates
{
    public DateOnly ValidFrom { get; }
    public decimal IbsState { get; }
    public decimal IbsMunicipal { get; }
    public decimal Cbs { get; }
    public string Source { get; }

    public IbsCbsRates(DateOnly validFrom, decimal ibsState, decimal ibsMunicipal, decimal cbs, string source)
    {
        ValidFrom = validFrom;
        IbsState = ValidRate(ibsState, "IBS estadual");
        IbsMunicipal = ValidRate(ibsMunicipal, "IBS municipal");
        Cbs = ValidRate(cbs, "CBS");
        Source = string.IsNullOrWhiteSpace(source)
            ? throw new DomainException("Informe a fonte das alíquotas (lei, resolução ou orientação do contador).")
            : source.Trim();
    }

    private static decimal ValidRate(decimal value, string field) =>
        value is >= 0m and <= 100m && decimal.Round(value, 4) == value
            ? value
            : throw new DomainException($"Alíquota de {field} deve estar entre 0 e 100%, com até 4 casas decimais.");
}

/// <summary>What the IBS/CBS group of one item carries. <see cref="Taxed"/> false: only CST and cClassTrib (exemption, immunity).</summary>
public sealed record IbsCbsTaxes(
    string Cst,
    string ClassTrib,
    bool Taxed,
    Money Base,
    decimal Reduction,
    decimal IbsStateRate,
    Money IbsState,
    decimal IbsMunicipalRate,
    Money IbsMunicipal,
    decimal CbsRate,
    Money Cbs)
{
    public Money Ibs => IbsState + IbsMunicipal;

    /// <summary>Rate after the reduction (<c>pAliqEfet</c>): rate × (1 − reduction / 100).</summary>
    public static decimal Effective(decimal rate, decimal reduction) => decimal.Round(rate * (1m - reduction / 100m), 4);
}

/// <summary>
/// IBS/CBS of one item, the same for every regime that has to fill the group (NT 2025.002-RTC, group UB).
/// Supported situations, to confirm with the accountant (PENDING.md):
/// - CST 000, full taxation: base × rate;
/// - CST 200, reduced rate: base × rate × (1 − reduction), the reduction coming from the tax rule;
/// - CST 400 (exemption) and 410 (immunity / no incidence): only CST and cClassTrib, no values.
/// Other situations (deferral, suspension, monophasic…) are refused until needed.
/// The base is the net item value minus ICMS, ISS, PIS and COFINS, which stay out of it during the transition
/// (LC 214/2025, art. 12 §2º; validation UB16-10). ICMS and ISS aren't charged on these items, so only PIS/COFINS are removed.
/// </summary>
public static class IbsCbsCalculation
{
    public static IbsCbsTaxes Calculate(FiscalItem item, PisCofins pis, PisCofins cofins, IbsCbsRates rates)
    {
        switch (item.IbsCbsCst)
        {
            case "400" or "410":
                return new IbsCbsTaxes(item.IbsCbsCst, item.ClassTrib, false, Money.Zero, 0m, 0m, Money.Zero, 0m, Money.Zero, 0m, Money.Zero);

            case "000" when item.IbsCbsReduction != 0m:
                throw new DomainException($"Item {item.Line}: CST de IBS/CBS 000 é tributação integral, mas a regra tem redução de {item.IbsCbsReduction}%.");

            case "200" when item.IbsCbsReduction <= 0m:
                throw new DomainException($"Item {item.Line}: CST de IBS/CBS 200 (alíquota reduzida) precisa do percentual de redução na regra tributária.");

            case "000" or "200":
                var taxBase = item.NetTotal - pis.Value - cofins.Value;
                var reduction = item.IbsCbsReduction;
                var ibsState = IbsCbsTaxes.Effective(rates.IbsState, reduction);
                var ibsMunicipal = IbsCbsTaxes.Effective(rates.IbsMunicipal, reduction);
                var cbs = IbsCbsTaxes.Effective(rates.Cbs, reduction);

                return new IbsCbsTaxes(
                    item.IbsCbsCst, item.ClassTrib, true, taxBase, reduction,
                    ibsState, TaxCalculator.Cents(taxBase.Amount * ibsState / 100m),
                    ibsMunicipal, TaxCalculator.Cents(taxBase.Amount * ibsMunicipal / 100m),
                    cbs, TaxCalculator.Cents(taxBase.Amount * cbs / 100m));

            default:
                throw new DomainException($"CST de IBS/CBS {item.IbsCbsCst} (item {item.Line}) ainda não é suportado na emissão.");
        }
    }
}

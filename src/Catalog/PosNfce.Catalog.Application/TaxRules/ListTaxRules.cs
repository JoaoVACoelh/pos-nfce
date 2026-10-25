using PosNfce.Catalog.Application.Ports;

namespace PosNfce.Catalog.Application.TaxRules;

/// <summary>What the product screen shows when the operator picks a tax rule.</summary>
public sealed record TaxRuleSummary(Guid Id, string Name, string Cfop, string Csosn);

public sealed class ListTaxRules(ITaxRuleRepository taxRules)
{
    public async Task<IReadOnlyList<TaxRuleSummary>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var rules = await taxRules.ListAsync(cancellationToken);

        return rules
            .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(r => new TaxRuleSummary(r.Id, r.Name, r.Cfop.Value, r.Csosn.Value))
            .ToList();
    }
}

using PosNfce.Catalog.Application.Ports;
using PosNfce.Catalog.Domain.TaxRules;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Application.TaxRules;

/// <summary>Raw values typed in the tax rule screen. They come from the accountant.</summary>
public sealed record RegisterTaxRuleCommand(
    string Name,
    string Cfop,
    string Csosn,
    string PisCst,
    decimal PisRate,
    string CofinsCst,
    decimal CofinsRate,
    string IbsCbsCst,
    string ClassTrib);

public sealed class RegisterTaxRule(ITaxRuleRepository taxRules)
{
    public async Task<Guid> ExecuteAsync(RegisterTaxRuleCommand command, CancellationToken cancellationToken = default)
    {
        var taxRule = TaxRule.Register(
            command.Name,
            Cfop.Parse(command.Cfop),
            Csosn.Parse(command.Csosn),
            PisCofinsCst.Parse(command.PisCst, "PIS"),
            Percentage.Of(command.PisRate, "PIS"),
            PisCofinsCst.Parse(command.CofinsCst, "COFINS"),
            Percentage.Of(command.CofinsRate, "COFINS"),
            TaxReformCodes.Parse(command.IbsCbsCst, command.ClassTrib));

        // The name is how the operator picks the rule in the product screen, so it must be unique.
        if (await taxRules.NameExistsAsync(taxRule.Name, cancellationToken))
        {
            throw new DomainException($"Já existe uma regra tributária chamada \"{taxRule.Name}\".");
        }

        await taxRules.AddAsync(taxRule, cancellationToken);
        return taxRule.Id;
    }
}

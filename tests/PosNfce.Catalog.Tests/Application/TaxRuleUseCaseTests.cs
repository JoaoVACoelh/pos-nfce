using PosNfce.Catalog.Adapters.Out.Persistence;
using PosNfce.Catalog.Application.TaxRules;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Tests.Application;

public sealed class TaxRuleUseCaseTests : IDisposable
{
    private readonly SqliteCatalog _database = new();

    public void Dispose() => _database.Dispose();

    private RegisterTaxRule NewRegister() => new(new TaxRuleRepository(_database.NewContext()));

    private ListTaxRules NewList() => new(new TaxRuleRepository(_database.NewContext()));

    [Fact]
    public async Task Registered_rules_are_listed_by_name()
    {
        await NewRegister().ExecuteAsync(ProductSamples.TaxRuleCommand("Padaria"));
        await NewRegister().ExecuteAsync(ProductSamples.TaxRuleCommand("Bebidas"));

        var rules = await NewList().ExecuteAsync();

        Assert.Equal(["Bebidas", "Padaria"], rules.Select(r => r.Name));
        Assert.All(rules, r => Assert.Equal("5102", r.Cfop));
    }

    [Fact]
    public async Task Rates_keep_four_decimal_places_after_a_round_trip()
    {
        await NewRegister().ExecuteAsync(ProductSamples.TaxRuleCommand() with { PisRate = 1.6543m, CofinsRate = 7.6m });

        var rule = (await new TaxRuleRepository(_database.NewContext()).ListAsync()).Single();

        Assert.Equal(1.6543m, rule.PisRate.Value);
        Assert.Equal(7.6m, rule.CofinsRate.Value);
    }

    [Fact]
    public async Task Two_rules_with_the_same_name_ignoring_case_are_not_allowed()
    {
        await NewRegister().ExecuteAsync(ProductSamples.TaxRuleCommand("Bebidas"));

        await Assert.ThrowsAsync<DomainException>(
            () => NewRegister().ExecuteAsync(ProductSamples.TaxRuleCommand("BEBIDAS")));
    }
}

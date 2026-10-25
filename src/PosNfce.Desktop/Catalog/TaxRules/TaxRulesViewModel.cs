using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Catalog.Application.TaxRules;
using PosNfce.Desktop.Shared;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Catalog.TaxRules;

/// <summary>Tax rules are filled with the data the accountant provides. This screen only registers new ones for now.</summary>
public sealed partial class TaxRulesViewModel(UseCaseRunner useCases) : FormViewModel
{
    public ObservableCollection<TaxRuleSummary> TaxRules { get; } = [];

    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Cfop { get; set; } = "";

    [ObservableProperty]
    public partial string Csosn { get; set; } = "";

    [ObservableProperty]
    public partial string PisCst { get; set; } = "";

    [ObservableProperty]
    public partial string PisRate { get; set; } = "0";

    [ObservableProperty]
    public partial string CofinsCst { get; set; } = "";

    [ObservableProperty]
    public partial string CofinsRate { get; set; } = "0";

    [ObservableProperty]
    public partial string IbsCbsCst { get; set; } = "";

    [ObservableProperty]
    public partial string ClassTrib { get; set; } = "";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var rules = await useCases.RunAsync<ListTaxRules, IReadOnlyList<TaxRuleSummary>>(u => u.ExecuteAsync());
        TaxRules.Clear();
        foreach (var rule in rules)
        {
            TaxRules.Add(rule);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var command = new RegisterTaxRuleCommand(
                Name, Cfop, Csosn,
                PisCst, ParseDecimal(PisRate, "Alíquota de PIS"),
                CofinsCst, ParseDecimal(CofinsRate, "Alíquota de COFINS"),
                IbsCbsCst, ClassTrib);

            await useCases.RunAsync<RegisterTaxRule, Guid>(u => u.ExecuteAsync(command));

            var registered = Name;
            Name = Cfop = Csosn = PisCst = CofinsCst = IbsCbsCst = ClassTrib = "";
            PisRate = CofinsRate = "0";
            await LoadAsync();
            ShowSuccess($"Regra \"{registered}\" cadastrada.");
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PosNfce.Desktop.Catalog.Ibpt;
using PosNfce.Desktop.Catalog.TaxRules;
using PosNfce.Desktop.Fiscal.IbsCbs;
using PosNfce.Desktop.Fiscal.Issuer;
using PosNfce.Desktop.Fiscal.Printer;

namespace PosNfce.Desktop.Settings;

/// <summary>
/// Screens set up once and rarely visited, kept out of the daily menu: the store, the printer and the tax tables.
/// Each tab creates a fresh page, like the side menu, so it always shows current data.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly NavigationItem _taxes = new("Impostos", typeof(TaxRulesViewModel));

    public SettingsViewModel(IServiceProvider services)
    {
        _services = services;
        Sections = [new("Emitente", typeof(IssuerViewModel)), new("Impressora", typeof(PrinterViewModel)), _taxes];
        Open(Sections[0]);
    }

    public IReadOnlyList<NavigationItem> Sections { get; }

    /// <summary>Second row of tabs, shown under "Impostos".</summary>
    public IReadOnlyList<NavigationItem> TaxSections { get; } =
    [
        new("Regras tributárias", typeof(TaxRulesViewModel)),
        new("Alíquotas IBS/CBS", typeof(IbsCbsRatesViewModel)),
        new("Tabela IBPT", typeof(IbptViewModel)),
    ];

    [ObservableProperty]
    public partial object? CurrentPage { get; set; }

    [ObservableProperty]
    public partial bool ShowTaxSections { get; set; }

    [RelayCommand]
    private void Open(NavigationItem section)
    {
        // Same page again: WPF would reuse its view for the new ViewModel without raising Loaded, leaving it empty.
        if (section.IsSelected && CurrentPage?.GetType() == section.PageType)
        {
            return;
        }

        foreach (var entry in Sections)
        {
            entry.IsSelected = entry == section;
        }

        ShowTaxSections = section == _taxes;
        if (ShowTaxSections)
        {
            OpenTax(TaxSections[0]);
            return;
        }

        CurrentPage = _services.GetRequiredService(section.PageType);
    }

    [RelayCommand]
    private void OpenTax(NavigationItem section)
    {
        if (section.IsSelected && CurrentPage?.GetType() == section.PageType)
        {
            return;
        }

        foreach (var entry in TaxSections)
        {
            entry.IsSelected = entry == section;
        }

        CurrentPage = _services.GetRequiredService(section.PageType);
    }
}

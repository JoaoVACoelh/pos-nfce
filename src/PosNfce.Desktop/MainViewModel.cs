using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PosNfce.Desktop.Catalog.Ibpt;
using PosNfce.Desktop.Catalog.Products;
using PosNfce.Desktop.Catalog.TaxRules;
using PosNfce.Desktop.Fiscal.Issuer;

namespace PosNfce.Desktop;

public sealed record NavigationItem(string Title, Type PageType);

/// <summary>Side menu and the page shown next to it. Each visit creates a fresh page, so it always shows current data.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;

    public MainViewModel(IServiceProvider services)
    {
        _services = services;
        Navigate(Menu[0]);
    }

    public IReadOnlyList<NavigationItem> Menu { get; } =
    [
        new("Produtos", typeof(ProductsViewModel)),
        new("Regras tributárias", typeof(TaxRulesViewModel)),
        new("Tabela IBPT", typeof(IbptViewModel)),
        new("Emitente", typeof(IssuerViewModel)),
    ];

    [ObservableProperty]
    public partial NavigationItem? SelectedMenu { get; set; }

    [ObservableProperty]
    public partial object? CurrentPage { get; set; }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        SelectedMenu = item;
        CurrentPage = _services.GetRequiredService(item.PageType);
    }
}

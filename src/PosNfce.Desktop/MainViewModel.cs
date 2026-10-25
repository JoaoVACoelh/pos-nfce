using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using PosNfce.Desktop.Checkout;
using PosNfce.Desktop.Catalog.Products;
using PosNfce.Desktop.Fiscal.Documents;
using PosNfce.Desktop.Settings;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop;

public sealed partial class NavigationItem(string title, Type pageType) : ObservableObject
{
    public string Title { get; } = title;
    public Type PageType { get; } = pageType;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>
/// Header, side menu and the page shown next to them. Each visit creates a fresh page, so it always shows current data.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly TimeProvider _clock;

    public MainViewModel(IServiceProvider services, TimeProvider clock)
    {
        _services = services;
        _clock = clock;
        Navigate(Menu[0]);

        UpdateClock();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        timer.Tick += (_, _) => UpdateClock();
        timer.Start();
    }

    public IReadOnlyList<NavigationItem> Menu { get; } =
    [
        new("Caixa", typeof(CheckoutViewModel)),
        new("Notas fiscais", typeof(FiscalDocumentsViewModel)),
        new("Produtos", typeof(ProductsViewModel)),
    ];

    /// <summary>Pinned to the bottom of the menu: issuer, printer and tax tables, set up once and rarely visited.</summary>
    public NavigationItem SettingsItem { get; } = new("Configurações", typeof(SettingsViewModel));

    [ObservableProperty]
    public partial object? CurrentPage { get; set; }

    /// <summary>The side menu can be hidden (hamburger button) to give the checkout more room on small screens.</summary>
    [ObservableProperty]
    public partial bool IsMenuOpen { get; set; } = true;

    [ObservableProperty]
    public partial string Clock { get; set; } = "";

    [RelayCommand]
    private void ToggleMenu() => IsMenuOpen = !IsMenuOpen;

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        // Same page again: WPF would reuse its view for the new ViewModel without raising Loaded, leaving it empty.
        if (item.IsSelected && CurrentPage?.GetType() == item.PageType)
        {
            return;
        }

        foreach (var entry in Menu.Append(SettingsItem))
        {
            entry.IsSelected = entry == item;
        }

        CurrentPage = _services.GetRequiredService(item.PageType);
    }

    private void UpdateClock() => Clock = BrazilTime.Now(_clock).ToString("ddd, dd/MM/yyyy  HH:mm");
}

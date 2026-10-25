using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Catalog.Application.Products;
using PosNfce.Catalog.Application.TaxRules;
using PosNfce.Desktop.Shared;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Catalog.Products;

/// <summary>Product list on the left, form on the right. Selecting a product loads it into the form for editing.</summary>
public sealed partial class ProductsViewModel(UseCaseRunner useCases) : FormViewModel
{
    public ObservableCollection<ProductDetails> Products { get; } = [];

    public ObservableCollection<TaxRuleSummary> TaxRules { get; } = [];

    public IReadOnlyList<OriginOption> Origins => OriginOption.All;

    [ObservableProperty]
    public partial string Search { get; set; } = "";

    [ObservableProperty]
    public partial ProductDetails? SelectedProduct { get; set; }

    /// <summary>Null while registering a new product.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing), nameof(FormTitle), nameof(ToggleActiveText))]
    public partial Guid? EditingId { get; set; }

    [ObservableProperty]
    public partial string Code { get; set; } = "";

    [ObservableProperty]
    public partial string Gtin { get; set; } = "";

    [ObservableProperty]
    public partial string Description { get; set; } = "";

    [ObservableProperty]
    public partial string Ncm { get; set; } = "";

    [ObservableProperty]
    public partial string Cest { get; set; } = "";

    [ObservableProperty]
    public partial string Unit { get; set; } = "UN";

    [ObservableProperty]
    public partial string Price { get; set; } = "";

    [ObservableProperty]
    public partial OriginOption? Origin { get; set; }

    [ObservableProperty]
    public partial TaxRuleSummary? TaxRule { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleActiveText))]
    public partial bool EditingActive { get; set; }

    public bool IsEditing => EditingId is not null;

    public string FormTitle => IsEditing ? "Editar produto" : "Novo produto";

    public string ToggleActiveText => EditingActive ? "Desativar" : "Reativar";

    public bool HasNoTaxRules => TaxRules.Count == 0;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var rules = await useCases.RunAsync<ListTaxRules, IReadOnlyList<TaxRuleSummary>>(u => u.ExecuteAsync());
        TaxRules.Clear();
        foreach (var rule in rules)
        {
            TaxRules.Add(rule);
        }

        OnPropertyChanged(nameof(HasNoTaxRules));
        await SearchAsync();
        New();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var found = await useCases.RunAsync<ListProducts, IReadOnlyList<ProductDetails>>(u => u.ExecuteAsync(Search));
        Products.Clear();
        foreach (var product in found)
        {
            Products.Add(product);
        }
    }

    [RelayCommand]
    private void New()
    {
        SelectedProduct = null;
        EditingId = null;
        Code = Gtin = Description = Ncm = Cest = Price = "";
        Unit = "UN";
        Origin = Origins[0];
        TaxRule = TaxRules.Count == 1 ? TaxRules[0] : null;
        EditingActive = true;
        Message = "";
    }

    partial void OnSelectedProductChanged(ProductDetails? value)
    {
        if (value is null)
        {
            return;
        }

        EditingId = value.Id;
        Code = value.Code;
        Gtin = value.Gtin;
        Description = value.Description;
        Ncm = value.Ncm;
        Cest = value.Cest ?? "";
        Unit = value.Unit;
        Price = value.Price.ToString("0.00##", Brazilian);
        Origin = Origins.Single(o => o.Value == value.Origin);
        TaxRule = TaxRules.SingleOrDefault(r => r.Id == value.TaxRuleId);
        EditingActive = value.Active;
        Message = "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var price = ParseDecimal(Price, "Preço");
            var origin = Origin?.Value ?? throw new DomainException("Escolha a origem da mercadoria.");
            var taxRuleId = TaxRule?.Id ?? throw new DomainException("Escolha a regra tributária.");

            if (EditingId is { } id)
            {
                await useCases.RunAsync<UpdateProduct>(u => u.ExecuteAsync(
                    new UpdateProductCommand(id, Gtin, Description, Ncm, Cest, Unit, price, origin, taxRuleId)));
                await SearchAsync();
                SelectedProduct = Products.SingleOrDefault(p => p.Id == id);
                ShowSuccess("Produto atualizado.");
            }
            else
            {
                await useCases.RunAsync<RegisterProduct, Guid>(u => u.ExecuteAsync(
                    new RegisterProductCommand(Code, Gtin, Description, Ncm, Cest, Unit, price, origin, taxRuleId)));
                var registered = Description;
                await SearchAsync();
                New();
                ShowSuccess($"\"{registered}\" cadastrado. Pode cadastrar o próximo.");
            }
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (EditingId is not { } id)
        {
            return;
        }

        await useCases.RunAsync<SetProductActive>(u => u.ExecuteAsync(id, !EditingActive));
        await SearchAsync();
        SelectedProduct = Products.SingleOrDefault(p => p.Id == id);
        ShowSuccess(EditingActive ? "Produto reativado." : "Produto desativado: não aparece mais no caixa.");
    }
}

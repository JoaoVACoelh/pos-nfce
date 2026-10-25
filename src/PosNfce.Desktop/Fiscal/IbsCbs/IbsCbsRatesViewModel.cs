using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Desktop.Shared;
using PosNfce.Fiscal.Application.Taxes;
using PosNfce.Fiscal.Domain.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Fiscal.IbsCbs;

/// <summary>IBS/CBS rate periods. They come from the law (via the accountant) and are never edited once registered.</summary>
public sealed partial class IbsCbsRatesViewModel(UseCaseRunner useCases) : FormViewModel
{
    public ObservableCollection<IbsCbsRates> Periods { get; } = [];

    [ObservableProperty] public partial string ValidFrom { get; set; } = "";
    [ObservableProperty] public partial string IbsState { get; set; } = "";
    [ObservableProperty] public partial string IbsMunicipal { get; set; } = "";
    [ObservableProperty] public partial string Cbs { get; set; } = "";
    [ObservableProperty] public partial string Source { get; set; } = "";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var periods = await useCases.RunAsync<ListIbsCbsRates, IReadOnlyList<IbsCbsRates>>(u => u.ExecuteAsync());
        Periods.Clear();
        foreach (var period in periods)
        {
            Periods.Add(period);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (!DateOnly.TryParseExact(ValidFrom.Trim(), "dd/MM/yyyy", Brazilian, DateTimeStyles.None, out var validFrom))
            {
                throw new DomainException("Data de início inválida: use dd/mm/aaaa, por exemplo 01/01/2027.");
            }

            var command = new RegisterIbsCbsRatesCommand(
                validFrom,
                ParseDecimal(IbsState, "IBS estadual"),
                ParseDecimal(IbsMunicipal, "IBS municipal"),
                ParseDecimal(Cbs, "CBS"),
                Source);

            await useCases.RunAsync<RegisterIbsCbsRates>(u => u.ExecuteAsync(command));

            ValidFrom = IbsState = IbsMunicipal = Cbs = Source = "";
            await LoadAsync();
            ShowSuccess($"Alíquotas a partir de {validFrom:dd/MM/yyyy} cadastradas.");
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
    }
}

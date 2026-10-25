using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Catalog.Application.Ibpt;
using PosNfce.Desktop.Shared;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Catalog.Ibpt;

/// <summary>Shows which IBPT table is in use and imports a new one.</summary>
public sealed partial class IbptViewModel(UseCaseRunner useCases) : FormViewModel
{
    [ObservableProperty] public partial string Status { get; set; } = "";
    [ObservableProperty] public partial bool NeedsAttention { get; set; }
    [ObservableProperty] public partial string FilePath { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    public partial bool IsImporting { get; set; }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var status = await useCases.RunAsync<GetIbptStatus, IbptStatus?>(u => u.ExecuteAsync());

        if (status is null)
        {
            Status = "Nenhuma tabela importada. O cupom vai sair sem o valor aproximado dos tributos.";
            NeedsAttention = true;
            return;
        }

        Status = $"Versão {status.Version}: {status.Count:N0} NCMs, vigência de {status.ValidFrom:dd/MM/yyyy} a {status.ValidUntil:dd/MM/yyyy}."
            + (status.Expired ? " Tabela vencida: baixe a versão atual." : "");
        NeedsAttention = status.Expired;
    }

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ImportAsync()
    {
        IsImporting = true;
        ShowSuccess("Importando...");
        try
        {
            if (FilePath.Trim().Length == 0)
            {
                throw new DomainException("Escolha o arquivo CSV da tabela IBPT.");
            }

            await using var file = File.OpenRead(FilePath.Trim());
            var result = await useCases.RunAsync<ImportIbptTable, IbptImportResult>(u => u.ExecuteAsync(file));

            FilePath = "";
            await LoadAsync();
            ShowSuccess($"Tabela {result.Version} importada: {result.Imported:N0} NCMs"
                + (result.SkippedRows > 0 ? $" ({result.SkippedRows:N0} linhas de serviço ignoradas)." : "."));
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
        catch (IOException error)
        {
            ShowError($"Não foi possível ler o arquivo: {error.Message}");
        }
        finally
        {
            IsImporting = false;
        }
    }

    private bool CanImport() => !IsImporting;
}

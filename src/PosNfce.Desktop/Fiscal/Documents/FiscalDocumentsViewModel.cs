using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Desktop.Shared;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Printing;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Fiscal.Documents;

public sealed record FiscalDocumentRow(
    Guid Id, DateTimeOffset SoldAt, string Number, int ItemCount, decimal Discount, decimal Total, string Status, string AccessKey,
    string LastMessage);

/// <summary>
/// NFC-e of the recent sales and where each one stands. A document can be sent to SEFAZ (homologation) or, while the real
/// certificate doesn't exist, only turned into a signed XML to check what will be sent.
/// </summary>
public sealed partial class FiscalDocumentsViewModel(UseCaseRunner useCases) : FormViewModel
{
    /// <summary>The view opens the preview window: windows are a view concern.</summary>
    public event Action<Danfce>? ShowDanfceRequested;

    public ObservableCollection<FiscalDocumentRow> Documents { get; } = [];

    [ObservableProperty] public partial string Summary { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrepareXmlCommand), nameof(IssueCommand), nameof(ShowDanfceCommand), nameof(PrintDanfceCommand))]
    public partial FiscalDocumentRow? SelectedDocument { get; set; }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var documents = await useCases.RunAsync<ListFiscalDocuments, IReadOnlyList<FiscalDocumentSummary>>(u => u.ExecuteAsync());

        Documents.Clear();
        foreach (var d in documents)
        {
            Documents.Add(new FiscalDocumentRow(
                d.Id, d.SoldAt, d.Number is null ? "—" : $"{d.Series}-{d.Number}", d.ItemCount, d.Discount, d.Total,
                Label(d.Status, d.AccessKey), d.AccessKey ?? "", d.LastMessage ?? ""));
        }

        var pending = documents.Count(d => d.Status == FiscalDocumentStatus.Pending);
        Summary = documents.Count == 0
            ? "Nenhuma venda concluída ainda."
            : $"{documents.Count} notas; {pending} aguardando emissão. Para enviar à SEFAZ (homologação), selecione a nota e clique em Emitir: só funciona com o certificado e o CSC reais da loja.";
    }

    /// <summary>Reserves the number (if needed) and writes the signed XML of the selected document. Nothing is sent.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task PrepareXmlAsync()
    {
        var id = SelectedDocument!.Id;
        try
        {
            var prepared = await useCases.RunAsync<PrepareNfceXml, PreparedNfce>(u => u.ExecuteAsync(id));
            await LoadAsync();
            SelectedDocument = Documents.FirstOrDefault(d => d.Id == id);
            ShowSuccess($"Nota {prepared.Series}-{prepared.Number} assinada e validada (não enviada). Chave {prepared.AccessKey}. Arquivo: {prepared.XmlPath}");
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
    }

    /// <summary>Sends the selected document to SEFAZ (homologation) and shows the answer.</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task IssueAsync()
    {
        var id = SelectedDocument!.Id;
        try
        {
            var result = await useCases.RunAsync<IssueNfce, IssueResult>(u => u.ExecuteAsync(id));
            await LoadAsync();
            SelectedDocument = Documents.FirstOrDefault(d => d.Id == id);
            if (result.Authorized)
            {
                ShowSuccess(result.Message);
            }
            else
            {
                ShowError(result.Message);
            }
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
    }

    /// <summary>Opens the DANFCE of the selected document (signed or authorized).</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ShowDanfceAsync()
    {
        var id = SelectedDocument!.Id;
        try
        {
            ShowDanfceRequested?.Invoke(await useCases.RunAsync<GetDanfce, Danfce>(u => u.ExecuteAsync(id)));
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
    }

    /// <summary>Prints the DANFCE of the selected document on the receipt printer (reprints included).</summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task PrintDanfceAsync()
    {
        var (id, number) = (SelectedDocument!.Id, SelectedDocument.Number);
        try
        {
            await useCases.RunAsync<PrintDanfce>(u => u.ExecuteAsync(id));
            ShowSuccess($"DANFCE da nota {number} enviado para a impressora.");
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
    }

    private bool HasSelection() => SelectedDocument is not null;

    private static string Label(FiscalDocumentStatus status, string? accessKey) => status switch
    {
        FiscalDocumentStatus.Pending when accessKey is not null => "XML assinado, aguardando envio",
        FiscalDocumentStatus.Pending => "Aguardando emissão",
        FiscalDocumentStatus.Authorized => "Autorizada",
        FiscalDocumentStatus.Rejected => "Rejeitada",
        FiscalDocumentStatus.Contingency => "Em contingência",
        FiscalDocumentStatus.Cancelled => "Cancelada",
        _ => status.ToString(),
    };
}

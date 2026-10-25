using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Desktop.Shared;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Domain.Documents;

namespace PosNfce.Desktop.Fiscal.Documents;

public sealed record FiscalDocumentRow(DateTimeOffset SoldAt, string Number, int ItemCount, decimal Discount, decimal Total, string Status);

/// <summary>NFC-e of the recent sales and where each one stands. Issuing comes in Stage 3.</summary>
public sealed partial class FiscalDocumentsViewModel(UseCaseRunner useCases) : ObservableObject
{
    public ObservableCollection<FiscalDocumentRow> Documents { get; } = [];

    [ObservableProperty] public partial string Summary { get; set; } = "";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var documents = await useCases.RunAsync<ListFiscalDocuments, IReadOnlyList<FiscalDocumentSummary>>(u => u.ExecuteAsync());

        Documents.Clear();
        foreach (var d in documents)
        {
            Documents.Add(new FiscalDocumentRow(
                d.SoldAt, d.Number is null ? "—" : $"{d.Series}-{d.Number}", d.ItemCount, d.Discount, d.Total, Label(d.Status)));
        }

        var pending = documents.Count(d => d.Status == FiscalDocumentStatus.Pending);
        Summary = documents.Count == 0
            ? "Nenhuma venda concluída ainda."
            : $"{documents.Count} notas; {pending} aguardando emissão. A emissão na SEFAZ entra na Etapa 3, com o certificado e o CSC da loja.";
    }

    private static string Label(FiscalDocumentStatus status) => status switch
    {
        FiscalDocumentStatus.Pending => "Aguardando emissão",
        FiscalDocumentStatus.Authorized => "Autorizada",
        FiscalDocumentStatus.Rejected => "Rejeitada",
        FiscalDocumentStatus.Contingency => "Em contingência",
        FiscalDocumentStatus.Cancelled => "Cancelada",
        _ => status.ToString(),
    };
}

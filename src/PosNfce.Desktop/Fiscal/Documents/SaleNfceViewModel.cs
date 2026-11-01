using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Desktop.Shared;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Printing;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Fiscal.Documents;

/// <summary>
/// NFC-e part of the "venda concluída" card in the checkout: the document's situation and the "Imprimir nota" button.
/// It lives in Fiscal so the checkout screen only hands over the sale id and never deals with SEFAZ.
/// With automatic issuing on, the NFC-e is sent in the background: the cashier can already scan the next customer.
/// </summary>
public sealed partial class SaleNfceViewModel(UseCaseRunner useCases) : FormViewModel
{
    // The technical reason (SEFAZ code and text) stays in "Notas fiscais"; the cashier only needs to know what to do.
    private const string NotIssued = "Não foi possível emitir a nota. Avise o responsável.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    public partial Guid? SaleId { get; set; }

    /// <summary>True while the automatic issuing talks to SEFAZ; the button waits, so the NFC-e is never sent twice.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    public partial bool IsSending { get; set; }

    [ObservableProperty] public partial string Status { get; set; } = "";

    [ObservableProperty] public partial string PrintText { get; set; } = "";

    /// <summary>"1 nota sem enviar": the card of an older sale is gone, so the checkout keeps this reminder.</summary>
    [ObservableProperty] public partial string UnsentNotice { get; set; } = "";

    /// <summary>Shows the sale's NFC-e and, when automatic issuing is on, starts sending it without waiting.</summary>
    public async Task LoadAsync(Guid saleId, string? afterSaleWarning)
    {
        SaleId = saleId;
        var nfce = await RefreshAsync();
        if (afterSaleWarning is not null)
        {
            ShowError(afterSaleWarning);
        }
        else
        {
            Message = "";
        }

        if (nfce?.WillAutoIssue == true)
        {
            _ = SendInBackgroundAsync(saleId);
        }
        else
        {
            await RefreshUnsentAsync();
        }
    }

    public void Clear()
    {
        SaleId = null;
        IsSending = false;
        Status = PrintText = Message = "";
    }

    public async Task RefreshUnsentAsync()
    {
        var count = await useCases.RunAsync<CountUnsentNfce, int>(u => u.ExecuteAsync());
        UnsentNotice = count switch
        {
            0 => "",
            1 => "1 nota sem enviar: veja em Notas fiscais",
            _ => $"{count} notas sem enviar: veja em Notas fiscais",
        };
    }

    private async Task SendInBackgroundAsync(Guid saleId)
    {
        IsSending = true;
        Status = "Enviando a nota...";
        try
        {
            var outcome = await useCases.RunAsync<AutoIssueSaleNfce, AutoIssueOutcome>(u => u.ExecuteAsync(saleId));
            // The card may already show the next sale: the document stays in "Notas fiscais" either way.
            if (SaleId == saleId && outcome.Sent)
            {
                if (outcome.PrintFailed)
                {
                    ShowError("Nota emitida, mas o cupom não saiu. Confira a impressora.");
                }
                else if (outcome.Authorized)
                {
                    Message = "";
                }
                else
                {
                    ShowError(NotIssued);
                }
            }
        }
        catch (Exception error)
        {
            // Nobody awaits this task: an error must reach the screen instead of being lost.
            if (SaleId == saleId)
            {
                ShowError($"{NotIssued} ({error.Message})");
            }
        }
        finally
        {
            if (SaleId == saleId)
            {
                IsSending = false;
            }
        }

        if (SaleId == saleId)
        {
            await RefreshAsync();
        }

        await RefreshUnsentAsync();
    }

    /// <summary>F12 or the button. Issues first when the document isn't authorized; prints only authorized documents.</summary>
    [RelayCommand(CanExecute = nameof(CanPrint))]
    private async Task PrintAsync()
    {
        if (SaleId is not { } saleId)
        {
            return;
        }

        try
        {
            ShowSuccess("Aguarde...");
            var result = await useCases.RunAsync<PrintSaleNfce, PrintSaleNfceResult>(u => u.ExecuteAsync(saleId));
            if (result.Printed)
            {
                ShowSuccess("Nota impressa.");
            }
            else
            {
                ShowError(NotIssued);
            }
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }

        await RefreshAsync();
        await RefreshUnsentAsync();
    }

    private bool CanPrint() => SaleId is not null && !IsSending;

    private async Task<SaleNfce?> RefreshAsync()
    {
        if (SaleId is not { } saleId)
        {
            return null;
        }

        var nfce = await useCases.RunAsync<GetSaleNfce, SaleNfce?>(u => u.ExecuteAsync(saleId));
        Status = nfce?.Status switch
        {
            FiscalDocumentStatus.Authorized => "Nota emitida.",
            FiscalDocumentStatus.Pending => "Nota ainda não enviada.",
            FiscalDocumentStatus.Contingency => "A nota será enviada depois.",
            FiscalDocumentStatus.Cancelled => "Nota cancelada.",
            _ => NotIssued,
        };
        PrintText = nfce?.Status == FiscalDocumentStatus.Authorized ? "Imprimir nota  (F12)" : "Tentar de novo  (F12)";
        return nfce;
    }
}

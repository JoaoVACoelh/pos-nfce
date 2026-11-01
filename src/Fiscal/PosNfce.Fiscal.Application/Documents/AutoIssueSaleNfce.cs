using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Application.Printing;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Documents;

/// <summary>
/// <paramref name="Sent"/> false: automatic issuing is off or the document isn't waiting to be sent, so nothing was done.
/// <paramref name="PrintFailed"/>: authorized, but the receipt didn't print; <paramref name="Message"/> says why.
/// </summary>
public sealed record AutoIssueOutcome(bool Sent, bool Authorized, bool PrintFailed, string Message)
{
    public static readonly AutoIssueOutcome NotSent = new(false, false, false, "");
}

/// <summary>
/// Automatic issuing, run by the checkout right after the sale is recorded and without holding it: sends the pending
/// NFC-e when the issuer has it turned on, then prints the DANFCE if the printer is set to print after authorization.
/// </summary>
public sealed class AutoIssueSaleNfce(
    IFiscalDocumentRepository documents, IIssuerRepository issuers, IssueNfce issueNfce,
    IReceiptPrinterSettingsStore printerSettings, PrintDanfce printDanfce)
{
    public async Task<AutoIssueOutcome> ExecuteAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var document = await documents.FindBySaleAsync(saleId, cancellationToken);
        if (document?.Status != FiscalDocumentStatus.Pending || (await issuers.GetAsync(cancellationToken))?.AutoIssue != true)
        {
            return AutoIssueOutcome.NotSent;
        }

        var result = await issueNfce.ExecuteAsync(document.Id, cancellationToken);
        if (!result.Authorized || !(await printerSettings.GetAsync(cancellationToken)).PrintAfterAuthorization)
        {
            return new AutoIssueOutcome(true, result.Authorized, false, result.Message);
        }

        try
        {
            await printDanfce.ExecuteAsync(document.Id, cancellationToken);
            return new AutoIssueOutcome(true, true, false, result.Message);
        }
        catch (DomainException error)
        {
            return new AutoIssueOutcome(true, true, true, $"NFC-e autorizada, mas o cupom não foi impresso: {error.Message}");
        }
    }
}

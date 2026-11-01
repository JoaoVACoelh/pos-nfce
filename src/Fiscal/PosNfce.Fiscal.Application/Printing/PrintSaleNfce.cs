using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Printing;

/// <summary><paramref name="Printed"/> false: SEFAZ didn't authorize the document and <paramref name="Message"/> says why.</summary>
public sealed record PrintSaleNfceResult(bool Printed, string Message);

/// <summary>
/// "Imprimir nota" at the checkout: prints the DANFCE of the sale, issuing the NFC-e first when it isn't authorized yet.
/// Nothing is printed for a document SEFAZ hasn't authorized: the customer would take home an invalid receipt.
/// </summary>
public sealed class PrintSaleNfce(IFiscalDocumentRepository documents, IssueNfce issueNfce, PrintDanfce printDanfce)
{
    public async Task<PrintSaleNfceResult> ExecuteAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var document = await documents.FindBySaleAsync(saleId, cancellationToken)
            ?? throw new DomainException("A nota desta venda não foi registrada. Confira em \"Notas fiscais\".");

        string? issued = null;
        if (document.Status != FiscalDocumentStatus.Authorized)
        {
            if (!document.CanBeIssued)
            {
                throw new DomainException("Esta nota não pode ser emitida nem impressa pelo caixa. Confira em \"Notas fiscais\".");
            }

            var result = await issueNfce.ExecuteAsync(document.Id, cancellationToken);
            if (!result.Authorized)
            {
                return new PrintSaleNfceResult(false, result.Message);
            }

            issued = $"NFC-e {result.Series}-{result.Number} autorizada";
        }

        await printDanfce.ExecuteAsync(document.Id, cancellationToken);
        return new PrintSaleNfceResult(true, issued is null ? "Nota impressa." : $"{issued} e impressa.");
    }
}

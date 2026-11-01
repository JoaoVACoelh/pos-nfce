using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Documents;

/// <summary>What the cashier needs to know about the NFC-e of a sale.</summary>
public sealed record IssueResult(SefazOutcome Outcome, int Series, long Number, string AccessKey, string Message)
{
    public bool Authorized => Outcome == SefazOutcome.Authorized;
}

/// <summary>
/// Issues a pending (or rejected and corrected) NFC-e: prepares the signed XML, sends it to SEFAZ and records the answer.
/// Authorized: the XML with the protocol is kept. Rejected: the number stays reserved for the corrected document.
/// Not sent: the document stays pending (Stage 4 adds contingency). Always homologation until Stage 6.
/// </summary>
public sealed class IssueNfce(
    PrepareNfceXml prepare,
    IFiscalDocumentRepository documents,
    IIssuerRepository issuers,
    IFiscalDocumentIssuer issuer,
    IXmlStore xmlStore)
{
    private const bool Homologation = true;

    public async Task<IssueResult> ExecuteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var prepared = await prepare.ExecuteAsync(documentId, cancellationToken);
        var store = await issuers.GetAsync(cancellationToken) ?? throw new DomainException("Cadastre o emitente antes de emitir notas.");

        var answer = await issuer.SendAsync(prepared.Signed, store.Certificate!, store.Profile, Homologation, cancellationToken);

        var document = await documents.FindAsync(documentId, cancellationToken) ?? throw new DomainException("Nota não encontrada.");
        switch (answer.Outcome)
        {
            case SefazOutcome.Authorized:
                document.Authorize(answer.Protocol!, answer.ReceivedAt ?? prepared.IssuedAt, answer.StatusCode ?? 100, answer.Message);
                await xmlStore.SaveAuthorizedAsync(prepared.AccessKey, prepared.IssuedAt, answer.AuthorizedXml!, cancellationToken);
                break;
            case SefazOutcome.Rejected:
                document.Reject(answer.StatusCode ?? 0, answer.Message);
                break;
            default:
                document.RecordSendFailure(answer.Message);
                break;
        }

        await documents.UpdateAsync(document, cancellationToken);

        var message = answer.Outcome switch
        {
            SefazOutcome.Authorized => $"NFC-e {prepared.Series}-{prepared.Number} autorizada. Protocolo {answer.Protocol}.",
            SefazOutcome.Rejected => $"NFC-e {prepared.Series}-{prepared.Number} rejeitada pela SEFAZ ({answer.StatusCode}): {answer.Message}",
            _ => $"NFC-e {prepared.Series}-{prepared.Number} não enviada: {answer.Message}",
        };
        return new IssueResult(answer.Outcome, prepared.Series, prepared.Number, prepared.AccessKey, message);
    }
}

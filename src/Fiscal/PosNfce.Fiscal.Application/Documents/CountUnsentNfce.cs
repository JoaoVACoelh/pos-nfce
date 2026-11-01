using PosNfce.Fiscal.Application.Ports;

namespace PosNfce.Fiscal.Application.Documents;

/// <summary>Sales whose NFC-e SEFAZ hasn't authorized yet (pending or rejected), for the notice at the checkout.</summary>
public sealed class CountUnsentNfce(IFiscalDocumentRepository documents)
{
    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default) => documents.CountUnsentAsync(cancellationToken);
}

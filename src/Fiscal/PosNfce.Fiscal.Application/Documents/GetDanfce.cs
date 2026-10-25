using PosNfce.Fiscal.Application.Ports;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Documents;

/// <summary>The DANFCE of a signed or authorized document, ready to preview or print.</summary>
public sealed class GetDanfce(IFiscalDocumentRepository documents, IIssuerRepository issuers)
{
    /// <summary>Always homologation until Stage 6, like the documents themselves.</summary>
    private const bool Homologation = true;

    public async Task<Danfce> ExecuteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await documents.FindAsync(documentId, cancellationToken) ?? throw new DomainException("Nota não encontrada.");
        var issuer = await issuers.GetAsync(cancellationToken) ?? throw new DomainException("Cadastre o emitente.");
        return DanfceLayout.Build(document, issuer.Profile, Homologation);
    }
}

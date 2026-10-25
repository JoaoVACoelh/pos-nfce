using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Documents;

public sealed record PreparedNfce(int Series, long Number, string AccessKey, string XmlPath, DateTimeOffset IssuedAt, SignedNfce Signed);

/// <summary>
/// Turns a pending document into a signed, schema-valid NFC-e XML: reserves the number (once), calculates the taxes,
/// builds the draft, signs it and keeps the file. Sending is done by <see cref="IssueNfce"/>, which calls this first.
/// Always homologation (tpAmb = 2) until Stage 6.
/// </summary>
public sealed class PrepareNfceXml(
    IFiscalDocumentRepository documents,
    IIssuerRepository issuers,
    IDocumentNumbering numbering,
    IApproximateTaxRates approximateRates,
    IIbsCbsRateRepository ibsCbsRates,
    IFiscalDocumentIssuer issuer,
    IXmlStore xmlStore,
    TimeProvider clock)
{
    private const bool Homologation = true;

    public async Task<PreparedNfce> ExecuteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var store = await issuers.GetAsync(cancellationToken) ?? throw new DomainException("Cadastre o emitente antes de emitir notas.");
        var certificate = store.Certificate ?? throw new DomainException("Envie o certificado digital na tela do emitente.");
        var csc = store.Csc ?? throw new DomainException("Cadastre o CSC na tela do emitente.");

        var document = await documents.FindAsync(documentId, cancellationToken) ?? throw new DomainException("Nota não encontrada.");
        if (!document.CanBeIssued)
        {
            throw new DomainException($"A nota {document.Series}-{document.Number} já foi {(document.Status == FiscalDocumentStatus.Authorized ? "autorizada" : "encerrada")}.");
        }
        if (document.Number is null)
        {
            await numbering.ReserveAsync(documentId, cancellationToken);
            document = (await documents.FindAsync(documentId, cancellationToken))!;
        }

        var issuedAt = BrazilTime.Now(clock);
        var issueDate = DateOnly.FromDateTime(issuedAt.DateTime);
        var ibsCbs = await ibsCbsRates.FindForDateAsync(issueDate, cancellationToken);

        var taxes = new List<ItemTaxes>();
        string? source = null;
        foreach (var item in document.Items)
        {
            var rates = await approximateRates.FindAsync(item.Ncm, item.Origin, cancellationToken);
            source ??= rates?.Source;
            taxes.Add(TaxCalculator.Default.Calculate(store.Profile.TaxRegime, item, rates?.Rates, ibsCbs, issueDate));
        }

        var draft = NfceDraft.Build(document, store.Profile, taxes, issuedAt, Homologation, NumericCode(document.Number!.Value), source);
        var signed = issuer.Sign(draft, certificate, csc);
        var path = await xmlStore.SaveSignedAsync(signed.AccessKey, issuedAt, signed.Xml, cancellationToken);

        document.RecordSignature(signed.AccessKey, signed.QrCode, signed.ConsultUrl, draft.AdditionalInfo);
        await documents.UpdateAsync(document, cancellationToken);

        return new PreparedNfce(document.Series!.Value, document.Number.Value, signed.AccessKey, path, issuedAt, signed);
    }

    /// <summary>cNF: 8 random digits that are part of the access key; the layout forbids it being equal to the number.</summary>
    private static string NumericCode(long number)
    {
        string code;
        do
        {
            code = Random.Shared.Next(0, 100_000_000).ToString("D8");
        }
        while (long.Parse(code) == number);

        return code;
    }
}

using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Issuers;

/// <summary>
/// Raw values from the issuer screen. CSC and certificate are optional: left empty, the stored ones are kept.
/// </summary>
public sealed record SaveIssuerCommand(
    string Cnpj,
    string StateRegistration,
    string LegalName,
    string? TradeName,
    string Street,
    string Number,
    string? Complement,
    string District,
    string CityName,
    string CityCode,
    string State,
    string PostalCode,
    string? Phone,
    int Series,
    long NextNumber,
    string? CscTokenId,
    string? Csc,
    byte[]? CertificatePfx,
    string? CertificatePassword,
    bool AutoIssue = false);

public sealed class SaveIssuer(
    IIssuerRepository issuers, IFiscalDocumentRepository documents, ICertificateReader certificates, TimeProvider clock)
{
    public async Task ExecuteAsync(SaveIssuerCommand command, CancellationToken cancellationToken = default)
    {
        var state = BrazilianState.Parse(command.State);
        var profile = new IssuerProfile(
            Cnpj.Parse(command.Cnpj),
            command.StateRegistration,
            command.LegalName,
            command.TradeName,
            TaxRegime.SimplesNacional,
            new IssuerAddress(
                command.Street, command.Number, command.Complement, command.District,
                command.CityName, command.CityCode, state, command.PostalCode),
            command.Phone);

        var issuer = await issuers.GetAsync(cancellationToken);
        if (issuer is null)
        {
            issuer = Issuer.Register(profile, command.Series, command.NextNumber);
        }
        else if (await documents.AnyNumberedAsync(cancellationToken))
        {
            // Numbers already went to documents: changing the series or going back would repeat numbers, and skipping
            // ahead leaves gaps that must be voided at SEFAZ. The screen shows them read-only; the stored ones are kept.
            issuer.Update(profile, issuer.Series, issuer.NextNumber);
        }
        else
        {
            issuer.Update(profile, command.Series, command.NextNumber);
        }

        // The screen never shows the stored CSC; an empty field means "keep it".
        if (!string.IsNullOrWhiteSpace(command.Csc))
        {
            issuer.ChangeCsc(new Csc(command.CscTokenId ?? "", command.Csc));
        }
        else if (!string.IsNullOrWhiteSpace(command.CscTokenId) && command.CscTokenId.Trim().TrimStart('0') != issuer.Csc?.TokenId)
        {
            throw new DomainException("Para trocar o ID do CSC, digite também o CSC novo.");
        }

        if (command.CertificatePfx is { Length: > 0 } pfx)
        {
            var password = command.CertificatePassword ?? "";
            var info = certificates.Read(pfx, password);
            issuer.AttachCertificate(new IssuerCertificate(pfx, password, info), clock.GetLocalNow());
        }

        issuer.SetAutoIssue(command.AutoIssue);

        // CSC and certificate may still be missing (they depend on the SEFAZ accreditation); issuing will require them.
        await issuers.SaveAsync(issuer, cancellationToken);
    }
}

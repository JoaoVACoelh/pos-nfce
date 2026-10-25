using PosNfce.Fiscal.Application.Ports;

namespace PosNfce.Fiscal.Application.Issuers;

/// <summary>Issuer data for the screen. Secrets (CSC, certificate password) never leave the module.</summary>
public sealed record IssuerDetails(
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
    bool HasCsc,
    CertificateSummary? Certificate,
    bool AutoIssue);

public sealed record CertificateSummary(string Cnpj, string Subject, DateTimeOffset NotAfter);

public sealed class GetIssuer(IIssuerRepository issuers)
{
    public async Task<IssuerDetails?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var issuer = await issuers.GetAsync(cancellationToken);
        if (issuer is null)
        {
            return null;
        }

        var profile = issuer.Profile;
        var address = profile.Address;
        var certificate = issuer.Certificate?.Info;

        return new IssuerDetails(
            profile.Cnpj.Value, profile.StateRegistration, profile.LegalName, profile.TradeName,
            address.Street, address.Number, address.Complement, address.District,
            address.CityName, address.CityCode, address.State.Code, address.PostalCode, profile.Phone,
            issuer.Series, issuer.NextNumber,
            issuer.Csc?.TokenId, issuer.Csc is not null,
            certificate is null ? null : new CertificateSummary(certificate.Cnpj.Formatted, certificate.Subject, certificate.NotAfter),
            issuer.AutoIssue);
    }
}

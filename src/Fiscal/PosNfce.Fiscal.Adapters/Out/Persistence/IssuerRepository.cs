using System.Text;
using Microsoft.EntityFrameworkCore;
using PosNfce.Fiscal.Adapters.Out.Security;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Issuers;

namespace PosNfce.Fiscal.Adapters.Out.Persistence;

/// <summary>Maps the issuer to its row, encrypting the secrets on the way in and decrypting them on the way out.</summary>
public sealed class IssuerRepository(FiscalDbContext db, ISecretProtector secrets) : IIssuerRepository
{
    public async Task<Issuer?> GetAsync(CancellationToken cancellationToken = default)
    {
        var record = await db.Issuers.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task SaveAsync(Issuer issuer, CancellationToken cancellationToken = default)
    {
        var record = ToRecord(issuer);
        var exists = await db.Issuers.AnyAsync(i => i.Id == issuer.Id, cancellationToken);

        if (exists)
        {
            db.Issuers.Update(record);
        }
        else
        {
            db.Issuers.Add(record);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private Issuer ToDomain(IssuerRecord r)
    {
        var state = BrazilianState.Parse(r.State);
        var profile = new IssuerProfile(
            Cnpj.Parse(r.Cnpj), r.StateRegistration, r.LegalName, r.TradeName, (TaxRegime)r.TaxRegime,
            new IssuerAddress(r.Street, r.Number, r.Complement, r.District, r.CityName, r.CityCode, state, r.PostalCode),
            r.Phone);

        var csc = r.CscEncrypted is null
            ? null
            : new Csc(r.CscTokenId ?? "", Decrypt(r.CscEncrypted));

        var certificate = r.CertificatePfxEncrypted is null
            ? null
            : new IssuerCertificate(
                secrets.Unprotect(r.CertificatePfxEncrypted),
                Decrypt(r.CertificatePasswordEncrypted!),
                new CertificateInfo(Cnpj.Parse(r.CertificateCnpj), r.CertificateSubject ?? "", r.CertificateNotAfter!.Value));

        return Issuer.Restore(r.Id, profile, r.Series, r.NextNumber, csc, certificate);
    }

    private IssuerRecord ToRecord(Issuer issuer)
    {
        var profile = issuer.Profile;
        var address = profile.Address;
        var certificate = issuer.Certificate;

        return new IssuerRecord
        {
            Id = issuer.Id,
            Cnpj = profile.Cnpj.Value,
            StateRegistration = profile.StateRegistration,
            LegalName = profile.LegalName,
            TradeName = profile.TradeName,
            TaxRegime = (int)profile.TaxRegime,
            Street = address.Street,
            Number = address.Number,
            Complement = address.Complement,
            District = address.District,
            CityName = address.CityName,
            CityCode = address.CityCode,
            State = address.State.Code,
            PostalCode = address.PostalCode,
            Phone = profile.Phone,
            Series = issuer.Series,
            NextNumber = issuer.NextNumber,
            CscTokenId = issuer.Csc?.TokenId,
            CscEncrypted = issuer.Csc is null ? null : Encrypt(issuer.Csc.Token),
            CertificatePfxEncrypted = certificate is null ? null : secrets.Protect(certificate.Pfx),
            CertificatePasswordEncrypted = certificate is null ? null : Encrypt(certificate.Password),
            CertificateCnpj = certificate?.Info.Cnpj.Value,
            CertificateSubject = certificate?.Info.Subject,
            CertificateNotAfter = certificate?.Info.NotAfter,
        };
    }

    private byte[] Encrypt(string text) => secrets.Protect(Encoding.UTF8.GetBytes(text));

    private string Decrypt(byte[] encrypted) => Encoding.UTF8.GetString(secrets.Unprotect(encrypted));
}

using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>
/// The store that issues the NFC-e. One per installation.
/// </summary>
public sealed class Issuer
{
    public const int MaxSeries = 999;
    public const long MaxNumber = 999_999_999;

    public Guid Id { get; }
    public IssuerProfile Profile { get; private set; }
    public Csc? Csc { get; private set; }
    public int Series { get; private set; }
    public long NextNumber { get; private set; }
    public IssuerCertificate? Certificate { get; private set; }

    private Issuer(Guid id, IssuerProfile profile, int series, long nextNumber)
    {
        Id = id;
        Profile = profile;
        (Series, NextNumber) = ValidNumbering(series, nextNumber);
    }

    public static Issuer Register(IssuerProfile profile, int series, long nextNumber) =>
        new(Guid.NewGuid(), profile, series, nextNumber);

    /// <summary>Rebuilds an issuer already stored, secrets already decrypted by the persistence adapter.</summary>
    public static Issuer Restore(Guid id, IssuerProfile profile, int series, long nextNumber, Csc? csc, IssuerCertificate? certificate) =>
        new(id, profile, series, nextNumber) { Csc = csc, Certificate = certificate };

    public void Update(IssuerProfile profile, int series, long nextNumber)
    {
        if (Certificate is not null)
        {
            EnsureSameCompany(profile.Cnpj, Certificate.Info);
        }

        Profile = profile;
        (Series, NextNumber) = ValidNumbering(series, nextNumber);
    }

    public void ChangeCsc(Csc csc) => Csc = csc;

    /// <summary>
    /// SEFAZ only accepts documents signed by a certificate of the same company (same CNPJ root) and still valid.
    /// Checking here gives the operator the error now, not at the first sale.
    /// </summary>
    public void AttachCertificate(IssuerCertificate certificate, DateTimeOffset now)
    {
        EnsureSameCompany(Profile.Cnpj, certificate.Info);

        if (certificate.Info.NotAfter <= now)
        {
            throw new DomainException($"O certificado venceu em {certificate.Info.NotAfter:dd/MM/yyyy}. Use um certificado válido.");
        }

        Certificate = certificate;
    }

    private static void EnsureSameCompany(Cnpj issuerCnpj, CertificateInfo certificate)
    {
        if (issuerCnpj.Root != certificate.Cnpj.Root)
        {
            throw new DomainException(
                $"O certificado é do CNPJ {certificate.Cnpj.Formatted}, de outra empresa que não a do emitente ({issuerCnpj.Formatted}).");
        }
    }

    private static (int, long) ValidNumbering(int series, long nextNumber)
    {
        if (series is < 0 or > MaxSeries)
        {
            throw new DomainException($"Série deve estar entre 0 e {MaxSeries}.");
        }

        if (nextNumber is < 1 or > MaxNumber)
        {
            throw new DomainException($"Próximo número deve estar entre 1 e {MaxNumber}.");
        }

        return (series, nextNumber);
    }
}

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>What the app knows about an A1 certificate after opening the .pfx with its password.</summary>
public sealed record CertificateInfo(Cnpj Cnpj, string Subject, DateTimeOffset NotAfter);

/// <summary>
/// The A1 certificate file and its password. Kept in plain form only in memory:
/// the persistence adapter encrypts both before writing them.
/// </summary>
public sealed record IssuerCertificate(byte[] Pfx, string Password, CertificateInfo Info);

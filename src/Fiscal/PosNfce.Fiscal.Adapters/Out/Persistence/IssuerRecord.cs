namespace PosNfce.Fiscal.Adapters.Out.Persistence;

/// <summary>Row of <c>fiscal_issuers</c>. Columns ending in "Encrypted" hold DPAPI-protected bytes.</summary>
public sealed class IssuerRecord
{
    public Guid Id { get; set; }
    public string Cnpj { get; set; } = "";
    public string StateRegistration { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string? TradeName { get; set; }
    public int TaxRegime { get; set; }
    public string Street { get; set; } = "";
    public string Number { get; set; } = "";
    public string? Complement { get; set; }
    public string District { get; set; } = "";
    public string CityName { get; set; } = "";
    public string CityCode { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string? Phone { get; set; }
    public int Series { get; set; }
    public long NextNumber { get; set; }

    public string? CscTokenId { get; set; }
    public byte[]? CscEncrypted { get; set; }

    public byte[]? CertificatePfxEncrypted { get; set; }
    public byte[]? CertificatePasswordEncrypted { get; set; }
    public string? CertificateCnpj { get; set; }
    public string? CertificateSubject { get; set; }
    public DateTimeOffset? CertificateNotAfter { get; set; }
}

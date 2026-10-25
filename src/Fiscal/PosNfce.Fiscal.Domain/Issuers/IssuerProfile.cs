using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>Registration data of the store that issues the NFC-e (<c>emit</c> group).</summary>
public sealed record IssuerProfile
{
    public Cnpj Cnpj { get; }

    /// <summary>Inscrição Estadual, digits only. Each state has its own check-digit rule, not validated yet.</summary>
    public string StateRegistration { get; }

    public string LegalName { get; }
    public string? TradeName { get; }
    public TaxRegime TaxRegime { get; }
    public IssuerAddress Address { get; }
    public string? Phone { get; }

    public IssuerProfile(
        Cnpj cnpj, string stateRegistration, string legalName, string? tradeName,
        TaxRegime taxRegime, IssuerAddress address, string? phone)
    {
        Cnpj = cnpj;

        StateRegistration = Text.Digits(stateRegistration);
        if (StateRegistration.Length is < 2 or > 14)
        {
            throw new DomainException("Inscrição Estadual deve ter entre 2 e 14 dígitos.");
        }

        LegalName = Text.Required(legalName, "Razão social", 60);
        TradeName = Text.Optional(tradeName, "Nome fantasia", 60);
        TaxRegime = Enum.IsDefined(taxRegime) ? taxRegime : throw new DomainException("Regime tributário não suportado.");
        Address = address;

        var phoneDigits = Text.Digits(phone);
        if (phoneDigits.Length is > 0 and (< 6 or > 14))
        {
            throw new DomainException("Telefone deve ter entre 6 e 14 dígitos, com DDD.");
        }

        Phone = phoneDigits.Length == 0 ? null : phoneDigits;
    }
}

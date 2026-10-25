using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>Registration data of the store that issues the NFC-e (<c>emit</c> group).</summary>
public sealed record IssuerProfile
{
    public Cnpj Cnpj { get; }

    /// <summary>Inscrição Estadual, digits only, checked by the rule of the store's state (<see cref="StateRegistrationRule"/>).</summary>
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

        StateRegistration = StateRegistrationRule.Validate(stateRegistration, address.State);

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

using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>Issuer address, NFC-e <c>enderEmit</c> group. Text limits follow the layout.</summary>
public sealed record IssuerAddress
{
    public string Street { get; }
    public string Number { get; }
    public string? Complement { get; }
    public string District { get; }
    public string CityName { get; }

    /// <summary>IBGE city code (<c>cMun</c>), 7 digits starting with the state's IBGE code.</summary>
    public string CityCode { get; }

    public BrazilianState State { get; }
    public string PostalCode { get; }

    public IssuerAddress(
        string street, string number, string? complement, string district,
        string cityName, string cityCode, BrazilianState state, string postalCode)
    {
        Street = Text.Required(street, "Logradouro", 60);
        Number = Text.Required(number, "Número", 60);
        Complement = Text.Optional(complement, "Complemento", 60);
        District = Text.Required(district, "Bairro", 60);
        CityName = Text.Required(cityName, "Município", 60);
        State = state;

        CityCode = Text.Digits(cityCode);
        if (CityCode.Length != 7 || !CityCode.StartsWith(state.IbgeCode.ToString(), StringComparison.Ordinal))
        {
            throw new DomainException($"Código IBGE do município deve ter 7 dígitos e começar com {state.IbgeCode} ({state.Code}).");
        }

        PostalCode = Text.Digits(postalCode);
        if (PostalCode.Length != 8)
        {
            throw new DomainException("CEP deve ter 8 dígitos.");
        }
    }
}

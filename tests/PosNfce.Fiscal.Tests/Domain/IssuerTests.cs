using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Domain;

public class IssuerTests
{
    // Fictitious CNPJs with valid check digits: same company (root 11222333), headquarters and branch, and another company.
    private const string Headquarters = "11222333000181";
    private const string Branch = "11222333000262";
    private const string OtherCompany = "11444777000161";

    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.FromHours(-3));

    internal static IssuerProfile Profile(string cnpj = Headquarters) => new(
        Cnpj.Parse(cnpj), "0623079040081", "Empresa Teste Ltda", null, TaxRegime.SimplesNacional,
        new IssuerAddress("Rua A", "100", null, "Centro", "Belo Horizonte", "3106200", BrazilianState.Parse("MG"), "30110-000"),
        "(31) 3333-4444");

    private static IssuerCertificate Certificate(string cnpj, DateTimeOffset notAfter) =>
        new([1, 2, 3], "x", new CertificateInfo(Cnpj.Parse(cnpj), "CN=TESTE", notAfter));

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Cnpj_accepts_valid_numbers_with_or_without_mask(string input)
    {
        Assert.Equal("11222333000181", Cnpj.Parse(input).Value);
    }

    [Theory]
    [InlineData("11222333000182")] // wrong check digit
    [InlineData("1122233300018")]  // 13 digits
    [InlineData("11111111111111")] // repeated digits pass the math but aren't valid
    public void Cnpj_rejects_invalid_numbers(string input)
    {
        Assert.Throws<DomainException>(() => Cnpj.Parse(input));
    }

    [Fact]
    public void City_code_must_belong_to_the_state()
    {
        // 3550308 is a São Paulo (35) code, not Minas Gerais (31).
        Assert.Throws<DomainException>(() =>
            new IssuerAddress("Rua A", "1", null, "Centro", "Cidade", "3550308", BrazilianState.Parse("MG"), "30110000"));
    }

    [Fact]
    public void Certificate_of_a_branch_of_the_same_company_is_accepted()
    {
        var issuer = Issuer.Register(Profile(Headquarters), 1, 1);

        issuer.AttachCertificate(Certificate(Branch, Now.AddYears(1)), Now);

        Assert.NotNull(issuer.Certificate);
    }

    [Fact]
    public void Certificate_of_another_company_is_rejected()
    {
        var issuer = Issuer.Register(Profile(Headquarters), 1, 1);

        var error = Assert.Throws<DomainException>(() => issuer.AttachCertificate(Certificate(OtherCompany, Now.AddYears(1)), Now));
        Assert.Contains("outra empresa", error.Message);
    }

    [Fact]
    public void Expired_certificate_is_rejected()
    {
        var issuer = Issuer.Register(Profile(), 1, 1);

        Assert.Throws<DomainException>(() => issuer.AttachCertificate(Certificate(Headquarters, Now.AddDays(-1)), Now));
    }

    [Fact]
    public void Changing_the_cnpj_to_another_company_after_attaching_a_certificate_is_rejected()
    {
        var issuer = Issuer.Register(Profile(Headquarters), 1, 1);
        issuer.AttachCertificate(Certificate(Headquarters, Now.AddYears(1)), Now);

        Assert.Throws<DomainException>(() => issuer.Update(Profile(OtherCompany), 1, 1));
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(1000, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1_000_000_000)]
    public void Series_and_number_must_fit_the_layout(int series, long number)
    {
        Assert.Throws<DomainException>(() => Issuer.Register(Profile(), series, number));
    }

    [Fact]
    public void Csc_id_drops_leading_zeros_and_csc_must_be_alphanumeric()
    {
        Assert.Equal("1", new Csc("000001", "ABC123").TokenId);
        Assert.Throws<DomainException>(() => new Csc("1", "ABC-123"));
    }
}

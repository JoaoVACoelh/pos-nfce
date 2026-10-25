using PosNfce.Fiscal.Adapters.Out.Certificates;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Adapters;

public class CertificateReaderTests
{
    private readonly X509CertificateReader _reader = new();
    private static readonly DateTimeOffset NextYear = DateTimeOffset.Now.AddYears(1);

    [Fact]
    public void Reads_cnpj_from_the_icp_brasil_subject_alt_name()
    {
        var pfx = TestCertificates.Create("11222333000181", NextYear, commonName: "EMPRESA SEM CNPJ NO CN");

        var info = _reader.Read(pfx, TestCertificates.Password);

        Assert.Equal("11222333000181", info.Cnpj.Value);
    }

    [Fact]
    public void Falls_back_to_the_cnpj_after_the_colon_in_the_common_name()
    {
        var pfx = TestCertificates.Create("11222333000181", NextYear, cnpjInSubjectAltName: false);

        Assert.Equal("11222333000181", _reader.Read(pfx, TestCertificates.Password).Cnpj.Value);
    }

    [Fact]
    public void Reads_the_expiration_date()
    {
        var pfx = TestCertificates.Create("11222333000181", NextYear);

        Assert.Equal(NextYear.Date, _reader.Read(pfx, TestCertificates.Password).NotAfter.Date);
    }

    [Fact]
    public void Wrong_password_gives_a_message_for_the_user()
    {
        var pfx = TestCertificates.Create("11222333000181", NextYear);

        var error = Assert.Throws<DomainException>(() => _reader.Read(pfx, "wrong"));
        Assert.Contains("senha incorreta", error.Message);
    }

    [Fact]
    public void Certificate_without_cnpj_is_rejected()
    {
        var pfx = TestCertificates.Create("11222333000181", NextYear, cnpjInSubjectAltName: false, commonName: "PESSOA FISICA");

        Assert.Throws<DomainException>(() => _reader.Read(pfx, TestCertificates.Password));
    }
}

using System.Runtime.Versioning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosNfce.Fiscal.Adapters.Out.Certificates;
using PosNfce.Fiscal.Adapters.Out.Persistence;
using PosNfce.Fiscal.Adapters.Out.Security;
using PosNfce.Fiscal.Application.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Application;

/// <summary>Use cases with the real adapters: SQLite in memory built from the migrations, real certificate reader and real DPAPI.</summary>
[SupportedOSPlatform("windows")]
public sealed class IssuerUseCaseTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DpapiSecretProtector _secrets = new();

    public IssuerUseCaseTests()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.Migrate();
    }

    public void Dispose() => _connection.Dispose();

    private FiscalDbContext NewContext() => new(new DbContextOptionsBuilder<FiscalDbContext>()
        .UseSqlite(_connection, sqlite => sqlite.MigrationsHistoryTable(FiscalDbContext.MigrationsHistoryTable))
        .Options);

    private SaveIssuer NewSave() => new(new IssuerRepository(NewContext(), _secrets), new X509CertificateReader(), TimeProvider.System);

    private GetIssuer NewGet() => new(new IssuerRepository(NewContext(), _secrets));

    private static SaveIssuerCommand Command(string? csc = "ABCDEF123456", byte[]? pfx = null) => new(
        Cnpj: "11.222.333/0001-81", StateRegistration: "0012345678901", LegalName: "Empresa Teste Ltda", TradeName: "Mercadinho Teste",
        Street: "Rua A", Number: "100", Complement: null, District: "Centro", CityName: "Belo Horizonte", CityCode: "3106200",
        State: "MG", PostalCode: "30110-000", Phone: null, Series: 1, NextNumber: 1,
        CscTokenId: "000001", Csc: csc, CertificatePfx: pfx, CertificatePassword: pfx is null ? null : TestCertificates.Password);

    [Fact]
    public async Task Saved_issuer_is_read_back_without_exposing_secrets()
    {
        var pfx = TestCertificates.Create("11222333000181", DateTimeOffset.Now.AddYears(1));

        await NewSave().ExecuteAsync(Command(pfx: pfx));
        var issuer = await NewGet().ExecuteAsync();

        Assert.NotNull(issuer);
        Assert.Equal(("11222333000181", "Mercadinho Teste", "MG"), (issuer.Cnpj, issuer.TradeName, issuer.State));
        Assert.Equal(("1", true), (issuer.CscTokenId, issuer.HasCsc));
        Assert.Equal("11.222.333/0001-81", issuer.Certificate!.Cnpj);
    }

    [Fact]
    public async Task Secrets_are_encrypted_in_the_database()
    {
        var pfx = TestCertificates.Create("11222333000181", DateTimeOffset.Now.AddYears(1));
        await NewSave().ExecuteAsync(Command(csc: "PLAINCSCVALUE", pfx: pfx));

        await using var db = NewContext();
        var row = await db.Issuers.SingleAsync();

        Assert.DoesNotContain("PLAINCSCVALUE", System.Text.Encoding.UTF8.GetString(row.CscEncrypted!));
        Assert.DoesNotContain(TestCertificates.Password, System.Text.Encoding.UTF8.GetString(row.CertificatePasswordEncrypted!));
        Assert.NotEqual(pfx, row.CertificatePfxEncrypted);
        Assert.Equal(pfx, _secrets.Unprotect(row.CertificatePfxEncrypted!));
    }

    [Fact]
    public async Task Saving_again_with_empty_csc_and_no_certificate_keeps_the_stored_ones()
    {
        var pfx = TestCertificates.Create("11222333000181", DateTimeOffset.Now.AddYears(1));
        await NewSave().ExecuteAsync(Command(pfx: pfx));

        await NewSave().ExecuteAsync(Command(csc: null) with { LegalName = "Nome Novo Ltda" });
        var issuer = await NewGet().ExecuteAsync();

        Assert.Equal("Nome Novo Ltda", issuer!.LegalName);
        Assert.True(issuer.HasCsc);
        Assert.NotNull(issuer.Certificate);
    }

    [Fact]
    public async Task Issuer_can_be_saved_before_csc_and_certificate_exist()
    {
        await NewSave().ExecuteAsync(Command(csc: null) with { CscTokenId = null });

        var issuer = await NewGet().ExecuteAsync();
        Assert.False(issuer!.HasCsc);
        Assert.Null(issuer.Certificate);
    }

    [Fact]
    public async Task Changing_the_csc_id_without_the_csc_is_rejected()
    {
        await NewSave().ExecuteAsync(Command());

        await Assert.ThrowsAsync<DomainException>(() => NewSave().ExecuteAsync(Command(csc: null) with { CscTokenId = "2" }));
    }

    [Fact]
    public async Task Certificate_of_another_company_is_not_saved()
    {
        var pfx = TestCertificates.Create("11444777000161", DateTimeOffset.Now.AddYears(1));

        await Assert.ThrowsAsync<DomainException>(() => NewSave().ExecuteAsync(Command(pfx: pfx)));
        Assert.Null(await NewGet().ExecuteAsync());
    }
}

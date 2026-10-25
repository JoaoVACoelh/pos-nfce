using System.Runtime.Versioning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PosNfce.Checkout.Application.Contracts;
using PosNfce.Fiscal.Adapters.Out.Certificates;
using PosNfce.Fiscal.Adapters.Out.Persistence;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Application;

/// <summary>Numbering lock after the first numbered NFC-e, and the consumer identification limit of MG.</summary>
[SupportedOSPlatform("windows")]
public sealed class IssuerNumberingTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("posnfce-numbering-").FullName;

    public IssuerNumberingTests()
    {
        using var db = NewContext();
        db.Database.Migrate();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }

    private FiscalDbContext NewContext() => new(new DbContextOptionsBuilder<FiscalDbContext>()
        .UseSqlite($"Data Source={Path.Combine(_folder, "pos.db")}", sqlite => sqlite.MigrationsHistoryTable(FiscalDbContext.MigrationsHistoryTable))
        .Options);

    private FiscalTestKit Kit() => new(NewContext, _folder);

    private static SaveIssuerCommand Command(int series = 1, long nextNumber = 1) => new(
        "11222333000181", "0623079040081", "EMPRESA TESTE LTDA", null, "Rua A", "100", null, "Centro", "Belo Horizonte",
        "3106200", "MG", "30110000", null, series, nextNumber, "1", "TESTE0123456789ABCDEF",
        TestCertificates.Create("11222333000181", DateTimeOffset.Now.AddYears(1)), TestCertificates.Password);

    private Task SaveAsync(SaveIssuerCommand command) =>
        new SaveIssuer(Kit().Issuers(), new FiscalDocumentRepository(NewContext()), new X509CertificateReader(), TimeProvider.System)
            .ExecuteAsync(command);

    private Task<IssuerDetails?> GetAsync() => new GetIssuer(Kit().Issuers(), new FiscalDocumentRepository(NewContext())).ExecuteAsync();

    private async Task<Guid> PendingAsync(decimal total, string? cpf = null)
    {
        await Kit().Handler().HandleAsync(new SaleCompleted(
            Guid.NewGuid(), DateTimeOffset.Now, total, 0m, total, 0m,
            [new SoldItem(1, Guid.NewGuid(), "P1", "SEM GTIN", "Produto", "UN", 1m, total, total,
                "22021000", null, 0, "5102", "102", "49", 0m, "49", 0m, "000", "000001", 0m)],
            [new ReceivedPayment("Cash", total, false)],
            cpf));
        return (await new FiscalDocumentRepository(NewContext()).ListRecentAsync(1)).Single().Id;
    }

    [Fact]
    public async Task Numbering_can_change_until_the_first_document_takes_a_number()
    {
        await SaveAsync(Command(series: 1, nextNumber: 1));
        await PendingAsync(10m); // pending, not numbered yet
        await SaveAsync(Command(series: 2, nextNumber: 50));

        var issuer = await GetAsync();

        Assert.Equal((2, 50L, false), (issuer!.Series, issuer.NextNumber, issuer.NumberingLocked));
    }

    [Fact]
    public async Task Numbering_is_kept_once_a_document_was_numbered()
    {
        await SaveAsync(Command(series: 1, nextNumber: 1));
        await Kit().Prepare().ExecuteAsync(await PendingAsync(10m)); // takes number 1

        await SaveAsync(Command(series: 5, nextNumber: 1) with { LegalName = "NOME NOVO LTDA" });

        var issuer = await GetAsync();
        Assert.Equal((1, 2L, true, "NOME NOVO LTDA"), (issuer!.Series, issuer.NextNumber, issuer.NumberingLocked, issuer.LegalName));
    }

    [Fact]
    public async Task Mg_document_of_ten_thousand_reais_without_cpf_is_not_signed()
    {
        await SaveAsync(Command());
        var id = await PendingAsync(10_000m);

        var error = await Assert.ThrowsAsync<DomainException>(() => Kit().Prepare().ExecuteAsync(id));

        Assert.Contains("CPF", error.Message);
    }

    [Fact]
    public async Task Mg_limit_is_offered_to_the_checkout()
    {
        Assert.Null(await new GetConsumerIdentificationLimit(Kit().Issuers()).ExecuteAsync()); // no issuer yet

        await SaveAsync(Command());

        Assert.Equal(10_000m, await new GetConsumerIdentificationLimit(Kit().Issuers()).ExecuteAsync());
    }
}

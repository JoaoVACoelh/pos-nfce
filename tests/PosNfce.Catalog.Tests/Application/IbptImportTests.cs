using System.Text;
using PosNfce.Catalog.Adapters.Out.Ibpt;
using PosNfce.Catalog.Adapters.Out.Persistence;
using PosNfce.Catalog.Application.Ibpt;
using PosNfce.Catalog.Domain.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Tests.Application;

/// <summary>Import of TestData/ibpt-sample.csv (fictitious values in the public IBPT layout) through the real reader and repository.</summary>
public sealed class IbptImportTests : IDisposable
{
    private static readonly string SamplePath = Path.Combine(AppContext.BaseDirectory, "TestData", "ibpt-sample.csv");
    private const string Header = "codigo;ex;tipo;descricao;nacionalfederal;importadosfederal;estadual;municipal;vigenciainicio;vigenciafim;chave;versao;fonte";

    private readonly SqliteCatalog _database = new();

    public void Dispose() => _database.Dispose();

    private ImportIbptTable NewImport() => new(new IbptCsvReader(), new IbptRateRepository(_database.NewContext()));

    private FindIbptRate NewFind() => new(new IbptRateRepository(_database.NewContext()));

    private static MemoryStream Csv(string body, Encoding? encoding = null) =>
        new((encoding ?? Encoding.UTF8).GetBytes($"{Header}\r\n{body}\r\n"));

    [Fact]
    public async Task Sample_file_imports_ncm_rows_and_skips_services()
    {
        await using var file = File.OpenRead(SamplePath);

        var result = await NewImport().ExecuteAsync(file);

        Assert.Equal((4, 2), (result.Imported, result.SkippedRows));
        Assert.Equal("26.2.A-TESTE", result.Version);
        Assert.Equal((new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 31)), (result.ValidFrom, result.ValidUntil));
    }

    [Fact]
    public async Task Federal_rate_depends_on_the_product_origin()
    {
        await using (var file = File.OpenRead(SamplePath))
        {
            await NewImport().ExecuteAsync(file);
        }

        var domestic = await NewFind().ExecuteAsync("2202.10.00", null, ProductOrigin.Domestic);
        var imported = await NewFind().ExecuteAsync("22021000", null, ProductOrigin.ForeignDirectImport);

        Assert.Equal((13.45m, 18.00m, 0m), (domestic!.Federal, domestic.State, domestic.Municipal));
        Assert.Equal(15.45m, imported!.Federal);
    }

    [Fact]
    public async Task Exception_code_is_a_separate_row()
    {
        await using (var file = File.OpenRead(SamplePath))
        {
            await NewImport().ExecuteAsync(file);
        }

        Assert.Equal(20.00m, (await NewFind().ExecuteAsync("22021000", "01", ProductOrigin.Domestic))!.Federal);
    }

    [Fact]
    public async Task Quoted_description_with_semicolon_is_read_correctly()
    {
        await using (var file = File.OpenRead(SamplePath))
        {
            await NewImport().ExecuteAsync(file);
        }

        Assert.Equal(4.20m, (await NewFind().ExecuteAsync("04069010", null, ProductOrigin.Domestic))!.Federal);
    }

    [Fact]
    public async Task Latin1_file_is_accepted()
    {
        var file = Csv("19052090;;0;Pães de forma;7.85;9.85;12.00;0.00;01/10/2026;31/12/2026;K;26.2.A;IBPT", Encoding.Latin1);

        Assert.Equal(1, (await NewImport().ExecuteAsync(file)).Imported);
    }

    [Fact]
    public async Task New_import_replaces_the_previous_table()
    {
        await NewImport().ExecuteAsync(Csv("22021000;;0;Antigo;10.00;10.00;10.00;0.00;01/01/2026;30/06/2026;K;26.1.A;IBPT"));
        await NewImport().ExecuteAsync(Csv("19052090;;0;Novo;7.85;9.85;12.00;0.00;01/07/2026;31/12/2026;K;26.2.A;IBPT"));

        Assert.Null(await NewFind().ExecuteAsync("22021000", null, ProductOrigin.Domestic));
        var status = await new GetIbptStatus(new IbptRateRepository(_database.NewContext()), TimeProvider.System).ExecuteAsync();
        Assert.Equal(("26.2.A", 1), (status!.Version, status.Count));
    }

    [Fact]
    public async Task Bad_line_is_reported_and_keeps_the_previous_table()
    {
        await NewImport().ExecuteAsync(Csv("22021000;;0;Antigo;10.00;10.00;10.00;0.00;01/01/2026;30/06/2026;K;26.1.A;IBPT"));

        var error = await Assert.ThrowsAsync<DomainException>(() => NewImport().ExecuteAsync(
            Csv("19052090;;0;Ok;7.85;9.85;12.00;0.00;01/07/2026;31/12/2026;K;26.2.A;IBPT\r\n19059090;;0;Ruim;abc;9.85;12.00;0.00;01/07/2026;31/12/2026;K;26.2.A;IBPT")));

        Assert.Contains("Linha 3", error.Message);
        Assert.NotNull(await NewFind().ExecuteAsync("22021000", null, ProductOrigin.Domestic));
    }

    [Fact]
    public async Task File_without_the_ibpt_columns_is_rejected()
    {
        var file = new MemoryStream(Encoding.UTF8.GetBytes("codigo;descricao\r\n22021000;Algo\r\n"));

        var error = await Assert.ThrowsAsync<DomainException>(() => NewImport().ExecuteAsync(file));
        Assert.Contains("layout", error.Message);
    }

    [Fact]
    public async Task Status_says_when_the_table_expired()
    {
        await NewImport().ExecuteAsync(Csv("22021000;;0;Antigo;10.00;10.00;10.00;0.00;01/01/2026;30/06/2026;K;26.1.A;IBPT"));

        var status = await new GetIbptStatus(new IbptRateRepository(_database.NewContext()), new FixedClock(new DateTime(2026, 7, 1))).ExecuteAsync();

        Assert.True(status!.Expired);
    }

    private sealed class FixedClock(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}

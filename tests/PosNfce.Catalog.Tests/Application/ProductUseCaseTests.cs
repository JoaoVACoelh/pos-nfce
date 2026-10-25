using PosNfce.Catalog.Adapters.Out.Persistence;
using PosNfce.Catalog.Application.Products;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Tests.Application;

/// <summary>Use cases running against the real repository and a SQLite database built from the migrations.</summary>
public sealed class ProductUseCaseTests : IDisposable
{
    private readonly SqliteCatalog _database = new();

    public ProductUseCaseTests()
    {
        // Every product needs an existing tax rule; the samples point to this one.
        using var db = _database.NewContext();
        db.TaxRules.Add(new TaxRuleRecord
        {
            Id = ProductSamples.TaxRuleId, Name = "Regra de teste", Cfop = "5102", Csosn = "102",
            PisCst = "49", CofinsCst = "49", IbsCbsCst = "000", ClassTrib = "000001",
        });
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    private RegisterProduct NewRegister()
    {
        var db = _database.NewContext();
        return new(new ProductRepository(db), new TaxRuleRepository(db));
    }

    private FindProductByBarcode NewFind() => new(new ProductRepository(_database.NewContext()));

    [Fact]
    public async Task Registered_product_is_found_by_its_gtin()
    {
        var id = await NewRegister().ExecuteAsync(ProductSamples.Command(gtin: "7891234567895"));

        var found = await NewFind().ExecuteAsync("7891234567895");

        Assert.NotNull(found);
        Assert.Equal(id, found.Id);
        Assert.Equal(new Money(9.90m), found.Price);
        Assert.Equal("UN", found.Unit);
    }

    [Fact]
    public async Task Product_without_barcode_is_found_by_its_code()
    {
        await NewRegister().ExecuteAsync(ProductSamples.Command(code: "PAO-FRANCES", gtin: null));

        var found = await NewFind().ExecuteAsync("PAO-FRANCES");

        Assert.NotNull(found);
        Assert.Equal("SEM GTIN", found.Gtin);
    }

    [Fact]
    public async Task Product_with_a_tax_rule_that_does_not_exist_is_rejected()
    {
        var error = await Assert.ThrowsAsync<DomainException>(
            () => NewRegister().ExecuteAsync(ProductSamples.Command(taxRuleId: Guid.NewGuid())));
        Assert.Contains("regra tributária", error.Message);
    }

    [Fact]
    public async Task Unknown_barcode_returns_nothing()
    {
        Assert.Null(await NewFind().ExecuteAsync("7891234567895"));
    }

    [Fact]
    public async Task Two_products_with_the_same_gtin_are_not_allowed()
    {
        await NewRegister().ExecuteAsync(ProductSamples.Command(code: "001"));

        var error = await Assert.ThrowsAsync<DomainException>(
            () => NewRegister().ExecuteAsync(ProductSamples.Command(code: "002")));
        Assert.Contains("GTIN", error.Message);
    }

    [Fact]
    public async Task Two_products_with_the_same_code_are_not_allowed()
    {
        await NewRegister().ExecuteAsync(ProductSamples.Command(code: "001", gtin: "7891234567895"));

        await Assert.ThrowsAsync<DomainException>(
            () => NewRegister().ExecuteAsync(ProductSamples.Command(code: "001", gtin: "12345670")));
    }

    [Fact]
    public async Task Many_products_without_barcode_can_coexist()
    {
        await NewRegister().ExecuteAsync(ProductSamples.Command(code: "001", gtin: null));
        await NewRegister().ExecuteAsync(ProductSamples.Command(code: "002", gtin: null));

        Assert.NotNull(await NewFind().ExecuteAsync("002"));
    }

    [Fact]
    public async Task Price_keeps_every_decimal_place_after_a_round_trip_to_the_database()
    {
        await NewRegister().ExecuteAsync(ProductSamples.Command() with { Price = 1234.5678m });

        var found = await NewFind().ExecuteAsync("7891234567895");

        Assert.Equal(1234.5678m, found!.Price.Amount);
    }
}

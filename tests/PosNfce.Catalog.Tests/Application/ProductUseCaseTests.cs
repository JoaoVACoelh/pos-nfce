using PosNfce.Catalog.Adapters.Out.Persistence;
using PosNfce.Catalog.Application.Products;
using PosNfce.Catalog.Domain.Products;
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

    private ListProducts NewList()
    {
        var db = _database.NewContext();
        return new(new ProductRepository(db), new TaxRuleRepository(db));
    }

    private UpdateProduct NewUpdate()
    {
        var db = _database.NewContext();
        return new(new ProductRepository(db), new TaxRuleRepository(db));
    }

    [Fact]
    public async Task Search_matches_description_code_or_gtin_ignoring_case()
    {
        await NewRegister().ExecuteAsync(ProductSamples.Command(code: "REF-01", gtin: "7891234567895") with { Description = "Refrigerante lata" });
        await NewRegister().ExecuteAsync(ProductSamples.Command(code: "PAO", gtin: null) with { Description = "Pao frances" });

        Assert.Equal(["Refrigerante lata"], (await NewList().ExecuteAsync("REFRI")).Select(p => p.Description));
        Assert.Equal(["Pao frances"], (await NewList().ExecuteAsync("pao")).Select(p => p.Description));
        Assert.Equal(["Refrigerante lata"], (await NewList().ExecuteAsync("4567895")).Select(p => p.Description));
        Assert.Equal(2, (await NewList().ExecuteAsync("")).Count);
    }

    [Fact]
    public async Task Search_treats_percent_sign_literally()
    {
        await NewRegister().ExecuteAsync(ProductSamples.Command() with { Description = "Produto comum" });

        Assert.Empty(await NewList().ExecuteAsync("%"));
    }

    [Fact]
    public async Task List_shows_the_tax_rule_name()
    {
        await NewRegister().ExecuteAsync(ProductSamples.Command());

        Assert.Equal("Regra de teste", (await NewList().ExecuteAsync(null)).Single().TaxRuleName);
    }

    [Fact]
    public async Task Updated_data_is_saved_but_code_and_gtin_stay()
    {
        var id = await NewRegister().ExecuteAsync(ProductSamples.Command(code: "001", gtin: "7891234567895"));

        await NewUpdate().ExecuteAsync(new UpdateProductCommand(
            id, "Novo nome", "11111111", "0300700", "kg", 15.75m, ProductOrigin.ForeignDirectImport, ProductSamples.TaxRuleId));

        var product = (await NewList().ExecuteAsync(null)).Single();
        Assert.Equal(("001", "7891234567895"), (product.Code, product.Gtin));
        Assert.Equal(("Novo nome", "11111111", "0300700", "KG", 15.75m), (product.Description, product.Ncm, product.Cest, product.Unit, product.Price));
        Assert.Equal(ProductOrigin.ForeignDirectImport, product.Origin);
    }

    [Fact]
    public async Task Deactivated_product_is_not_found_at_the_checkout_but_stays_in_the_list()
    {
        var id = await NewRegister().ExecuteAsync(ProductSamples.Command(gtin: "7891234567895"));

        await new SetProductActive(new ProductRepository(_database.NewContext())).ExecuteAsync(id, active: false);

        Assert.Null(await NewFind().ExecuteAsync("7891234567895"));
        Assert.False((await NewList().ExecuteAsync(null)).Single().Active);
    }

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

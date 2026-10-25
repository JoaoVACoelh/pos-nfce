// Fills the local database with FICTITIOUS data for development: an issuer, tax rules, products and the sample
// IBPT table. Everything goes through the real use cases, so the same validations as the screens apply.
// Tax values are format examples marked "(TESTE)", NOT real classifications: real ones come from the accountant.
//
// Usage, from the repository root (close nothing, the app can stay open):
//   dotnet run tools/create-dev-certificate.cs     (once, for the issuer certificate)
//   dotnet run tools/seed-dev-data.cs
// Target: %LOCALAPPDATA%\PosNfce\pos.db, or the folder in POSNFCE_DATA_FOLDER.
// Running it again skips existing tax rules, updates existing products and keeps the issuer's stored secrets.

#:project ../src/Catalog/PosNfce.Catalog.Adapters/PosNfce.Catalog.Adapters.csproj
#:project ../src/Fiscal/PosNfce.Fiscal.Adapters/PosNfce.Fiscal.Adapters.csproj
#:package Microsoft.Extensions.Hosting
// File-based apps default to NativeAOT, which EF Core's runtime model building doesn't support.
#:property PublishAot=false

using Microsoft.Extensions.DependencyInjection;
using PosNfce.Catalog.Adapters;
using PosNfce.Catalog.Application.Ibpt;
using PosNfce.Catalog.Application.Products;
using PosNfce.Catalog.Application.TaxRules;
using PosNfce.Catalog.Domain.Products;
using PosNfce.Fiscal.Adapters;
using PosNfce.Fiscal.Application.Issuers;
using PosNfce.Fiscal.Application.Taxes;
using PosNfce.SharedKernel;

var root = Directory.GetCurrentDirectory();
var dataFolder = Directory.CreateDirectory(Environment.GetEnvironmentVariable("POSNFCE_DATA_FOLDER")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PosNfce")).FullName;
var connectionString = $"Data Source={Path.Combine(dataFolder, "pos.db")}";
Console.WriteLine($"Database: {Path.Combine(dataFolder, "pos.db")}");

var services = new ServiceCollection()
    .AddCatalogModule(connectionString)
    .AddFiscalModule(connectionString)
    .BuildServiceProvider();
await services.MigrateCatalogDatabaseAsync();
await services.MigrateFiscalDatabaseAsync();

async Task Run<T>(string what, Func<T, Task> action) where T : notnull
{
    await using var scope = services.CreateAsyncScope();
    try
    {
        await action(scope.ServiceProvider.GetRequiredService<T>());
        Console.WriteLine($"  ok      {what}");
    }
    catch (DomainException error)
    {
        Console.WriteLine($"  skipped {what}: {error.Message}");
    }
}

// --- Issuer (dev certificate CNPJ, fictitious address and CSC) -----------------------------------------------
Console.WriteLine("Issuer");
var certificatePath = Path.Combine(root, "data", "dev-certificate.pfx");
var certificate = File.Exists(certificatePath) ? await File.ReadAllBytesAsync(certificatePath) : null;
if (certificate is null)
{
    Console.WriteLine("  (no data/dev-certificate.pfx: issuer saved without certificate; run tools/create-dev-certificate.cs)");
}

await Run<SaveIssuer>("Mercadinho Exemplo (TESTE)", u => u.ExecuteAsync(new SaveIssuerCommand(
    Cnpj: "11.222.333/0001-81", StateRegistration: "0000000000000", LegalName: "MERCADINHO EXEMPLO LTDA (TESTE)",
    TradeName: "Mercadinho Exemplo", Street: "Rua de Teste", Number: "123", Complement: null, District: "Centro",
    CityName: "Belo Horizonte", CityCode: "3106200", State: "MG", PostalCode: "30110-000", Phone: "(31) 3000-0000",
    Series: 1, NextNumber: 1, CscTokenId: "000001", Csc: "TESTE0123456789ABCDEF",
    CertificatePfx: certificate, CertificatePassword: certificate is null ? null : "dev-only")));

// --- Tax rules (format examples, NOT validated by an accountant) ---------------------------------------------
Console.WriteLine("Tax rules");
const string General = "Mercadorias em geral (TESTE)";
const string WithSt = "Bebidas com ST (TESTE)";
const string Monophasic = "Higiene com PIS/COFINS monofásico (TESTE)";

await Run<RegisterTaxRule>(General, u => u.ExecuteAsync(new RegisterTaxRuleCommand(
    General, Cfop: "5102", Csosn: "102", PisCst: "49", PisRate: 0, CofinsCst: "49", CofinsRate: 0, IbsCbsCst: "000", ClassTrib: "000001")));
await Run<RegisterTaxRule>(WithSt, u => u.ExecuteAsync(new RegisterTaxRuleCommand(
    WithSt, Cfop: "5405", Csosn: "500", PisCst: "49", PisRate: 0, CofinsCst: "49", CofinsRate: 0, IbsCbsCst: "000", ClassTrib: "000001")));
await Run<RegisterTaxRule>(Monophasic, u => u.ExecuteAsync(new RegisterTaxRuleCommand(
    Monophasic, Cfop: "5405", Csosn: "500", PisCst: "04", PisRate: 0, CofinsCst: "04", CofinsRate: 0, IbsCbsCst: "000", ClassTrib: "000001")));

Dictionary<string, Guid> rules;
await using (var scope = services.CreateAsyncScope())
{
    rules = (await scope.ServiceProvider.GetRequiredService<ListTaxRules>().ExecuteAsync()).ToDictionary(r => r.Name, r => r.Id);
}

// --- Products ------------------------------------------------------------------------------------------------
// GTINs start with "200": GS1 reserves prefix 2 for in-store use, so they never match a real product.
Console.WriteLine("Products");
var products = new (string Code, bool Barcode, string Description, string Unit, decimal Price, string Ncm, string Rule)[]
{
    ("BEB-001", true,  "Refrigerante cola 2 L (TESTE)",         "UN", 9.49m,  "22021000", WithSt),
    ("BEB-002", true,  "Água mineral sem gás 500 ml (TESTE)",   "UN", 2.50m,  "22011000", WithSt),
    ("BEB-003", true,  "Cerveja lata 350 ml (TESTE)",           "UN", 4.29m,  "22030000", WithSt),
    ("PAD-001", false, "Pão francês (TESTE)",                   "KG", 16.90m, "19059090", General),
    ("PAD-002", false, "Bolo de fubá (TESTE)",                  "UN", 18.00m, "19059090", General),
    ("MER-001", true,  "Arroz branco tipo 1 5 kg (TESTE)",      "UN", 27.90m, "10063021", General),
    ("MER-002", true,  "Feijão carioca 1 kg (TESTE)",           "UN", 8.99m,  "07133399", General),
    ("MER-003", true,  "Açúcar cristal 1 kg (TESTE)",           "UN", 4.79m,  "17019900", General),
    ("MER-004", true,  "Café torrado e moído 500 g (TESTE)",    "UN", 19.90m, "09012100", General),
    ("MER-005", true,  "Óleo de soja 900 ml (TESTE)",           "UN", 7.49m,  "15079011", General),
    ("LAT-001", true,  "Leite UHT integral 1 L (TESTE)",        "UN", 5.29m,  "04012010", General),
    ("LAT-002", false, "Queijo minas frescal (TESTE)",          "KG", 42.90m, "04069010", General),
    ("HIG-001", true,  "Sabonete em barra 90 g (TESTE)",        "UN", 2.99m,  "34011190", Monophasic),
    ("HIG-002", true,  "Creme dental 90 g (TESTE)",             "UN", 4.49m,  "33061000", Monophasic),
};

var sequence = 1;
foreach (var p in products)
{
    var gtin = p.Barcode ? FictitiousGtin(sequence++) : null;
    var label = $"{p.Code}  {gtin ?? "SEM GTIN",-13}  {p.Description}";

    // Already there from a previous run: bring description, price and tax data up to date (code and GTIN never change).
    Guid? existing;
    await using (var scope = services.CreateAsyncScope())
    {
        existing = (await scope.ServiceProvider.GetRequiredService<ListProducts>().ExecuteAsync(p.Code))
            .SingleOrDefault(x => x.Code == p.Code)?.Id;
    }

    if (existing is { } id)
    {
        await Run<UpdateProduct>($"{label} (updated)", u => u.ExecuteAsync(new UpdateProductCommand(
            id, p.Description, p.Ncm, Cest: null, p.Unit, p.Price, ProductOrigin.Domestic, rules[p.Rule])));
    }
    else
    {
        await Run<RegisterProduct>(label, u => u.ExecuteAsync(new RegisterProductCommand(
            p.Code, gtin, p.Description, p.Ncm, Cest: null, p.Unit, p.Price, ProductOrigin.Domestic, rules[p.Rule])));
    }
}

// --- IBS/CBS rates: 2026 test year (IBS 0,1% state, 0% municipal, CBS 0,9%). Only 2026 is registered: the rates of
// later years depend on the Senate's reference rates and must come from the accountant. --------------------------------
Console.WriteLine("IBS/CBS rates");
await Run<RegisterIbsCbsRates>("2026 test year", u => u.ExecuteAsync(new RegisterIbsCbsRatesCommand(
    new DateOnly(2026, 1, 1), IbsState: 0.1m, IbsMunicipal: 0m, Cbs: 0.9m,
    Source: "LC 214/2025, ano-teste 2026 (confirmar com o contador)")));

// --- IBPT (sample file, fictitious rates) --------------------------------------------------------------------
Console.WriteLine("IBPT");
await Run<ImportIbptTable>("sample table", async u =>
{
    await using var file = File.OpenRead(Path.Combine(root, "tests", "PosNfce.Catalog.Tests", "TestData", "ibpt-sample.csv"));
    await u.ExecuteAsync(file);
});

Console.WriteLine("Done. Open (or switch screens in) the app to see the data.");

// GTIN-13 "200" + 9-digit sequence + GS1 mod-10 check digit.
static string FictitiousGtin(int sequence)
{
    var digits = $"200{sequence:D9}";
    var sum = digits.Reverse().Select((c, i) => (c - '0') * (i % 2 == 0 ? 3 : 1)).Sum();
    return digits + (10 - sum % 10) % 10;
}

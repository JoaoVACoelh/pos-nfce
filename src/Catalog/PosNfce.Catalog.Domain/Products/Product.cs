using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.Products;

/// <summary>
/// A product sold at the checkout. Limits follow the NFC-e layout: <c>cProd</c> up to 60 characters,
/// <c>xProd</c> up to 120 and <c>uCom</c> up to 6.
/// </summary>
public sealed class Product
{
    public const int CodeMaxLength = 60;
    public const int DescriptionMaxLength = 120;
    public const int UnitMaxLength = 6;

    public Guid Id { get; }

    /// <summary>Store's internal code (<c>cProd</c>). Products without a barcode are found by it.</summary>
    public string Code { get; }

    public Gtin Gtin { get; private set; }
    public string Description { get; private set; }
    public Ncm Ncm { get; private set; }
    public Cest? Cest { get; private set; }
    public string Unit { get; private set; }
    public Money Price { get; private set; }
    public ProductOrigin Origin { get; private set; }
    public Guid TaxRuleId { get; private set; }
    public bool Active { get; private set; }

    private Product(
        Guid id, string code, Gtin gtin, string description, Ncm ncm, Cest? cest,
        string unit, Money price, ProductOrigin origin, Guid taxRuleId, bool active)
    {
        Id = id;
        Code = Required(code, "Código", CodeMaxLength);
        Gtin = gtin;
        Description = Required(description, "Descrição", DescriptionMaxLength);
        Ncm = ncm;
        Cest = cest;
        Unit = Required(unit, "Unidade", UnitMaxLength).ToUpperInvariant();
        Price = PositivePrice(price);
        Origin = ValidOrigin(origin);
        TaxRuleId = taxRuleId != Guid.Empty ? taxRuleId : throw new DomainException("Informe a regra tributária do produto.");
        Active = active;
    }

    public static Product Register(
        string code, Gtin gtin, string description, Ncm ncm, Cest? cest,
        string unit, Money price, ProductOrigin origin, Guid taxRuleId) =>
        new(Guid.NewGuid(), code, gtin, description, ncm, cest, unit, price, origin, taxRuleId, active: true);

    /// <summary>Rebuilds a product already stored. Runs the same validations as <see cref="Register"/>.</summary>
    public static Product Restore(
        Guid id, string code, Gtin gtin, string description, Ncm ncm, Cest? cest,
        string unit, Money price, ProductOrigin origin, Guid taxRuleId, bool active) =>
        new(id, code, gtin, description, ncm, cest, unit, price, origin, taxRuleId, active);

    /// <summary>
    /// Changes the editable data. Only the internal code stays fixed: it is how the store identifies the product.
    /// Changing the GTIN (e.g. the supplier changed the package) doesn't affect past sales, which keep their own copy.
    /// </summary>
    public void Update(Gtin gtin, string description, Ncm ncm, Cest? cest, string unit, Money price, ProductOrigin origin, Guid taxRuleId)
    {
        Gtin = gtin;
        Description = Required(description, "Descrição", DescriptionMaxLength);
        Ncm = ncm;
        Cest = cest;
        Unit = Required(unit, "Unidade", UnitMaxLength).ToUpperInvariant();
        Price = PositivePrice(price);
        Origin = ValidOrigin(origin);
        TaxRuleId = taxRuleId != Guid.Empty ? taxRuleId : throw new DomainException("Informe a regra tributária do produto.");
    }

    public void ChangePrice(Money newPrice) => Price = PositivePrice(newPrice);

    public void Deactivate() => Active = false;

    public void Activate() => Active = true;

    private static string Required(string value, string field, int maxLength)
    {
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            throw new DomainException($"{field} é obrigatório.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{field} pode ter no máximo {maxLength} caracteres.");
        }

        return trimmed;
    }

    private static Money PositivePrice(Money price) =>
        price > Money.Zero ? price : throw new DomainException("O preço deve ser maior que zero.");

    private static ProductOrigin ValidOrigin(ProductOrigin origin) =>
        Enum.IsDefined(origin) ? origin : throw new DomainException($"Origem da mercadoria {(int)origin} inválida.");
}

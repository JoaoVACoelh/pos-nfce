using System.Globalization;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Domain.Sales;

/// <summary>
/// What the cashier typed or scanned in the checkout field: a code, optionally preceded by a quantity and "*".
/// Examples: "7891234567895", "3*7891234567895", "0,350*PAO" (350 g of a product sold by kg).
/// </summary>
public sealed record ScanInput(decimal Quantity, string Code, bool QuantityTyped = false)
{
    /// <summary>Keeps a barcode read in the quantity field from becoming a quantity.</summary>
    private const decimal MaxQuantity = 99_999m;

    public static ScanInput Parse(string? typed)
    {
        var text = typed?.Trim() ?? "";
        var star = text.IndexOf('*');

        if (star < 0)
        {
            return text.Length > 0 ? new ScanInput(1m, text) : throw new DomainException("Leia ou digite o código do produto.");
        }

        var quantityText = text[..star].Trim().Replace('.', ',');
        var code = text[(star + 1)..].Trim();

        if (!decimal.TryParse(quantityText, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("pt-BR"), out var quantity)
            || quantity <= 0 || quantity > MaxQuantity)
        {
            throw new DomainException($"Quantidade \"{text[..star]}\" inválida. Use, por exemplo, 3*código.");
        }

        if (code.Length == 0)
        {
            throw new DomainException("Depois da quantidade e do *, leia ou digite o código do produto.");
        }

        return new ScanInput(quantity, code, QuantityTyped: true);
    }

    /// <summary>The quantity of the item. Without a typed one, a product sold by weight would silently become 1 kg.</summary>
    public decimal QuantityFor(string unit, string description) =>
        !QuantityTyped && ScaleLabel.SoldByWeight(unit) ? throw new WeightRequiredException(description) : Quantity;
}

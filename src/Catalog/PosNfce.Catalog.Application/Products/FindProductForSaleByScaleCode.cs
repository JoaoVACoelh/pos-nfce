using System.Globalization;
using PosNfce.Catalog.Application.Ports;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Application.Products;

/// <summary>
/// Finds the product a labeling scale label points to. The scale prints the product's internal code as a number,
/// zero-padded to its field, so item 5 matches the codes "5", "05" … "000005", whatever width the scale uses.
/// Used by the Checkout module (through its own port) when no GTIN or code matched the scanned label.
/// </summary>
public sealed class FindProductForSaleByScaleCode(IProductRepository products, ITaxRuleRepository taxRules)
{
    /// <summary>Width of the code field on the label (EAN-13 "2 CCCCCC WWWWW D").</summary>
    private const int MaxDigits = 6;

    public async Task<ProductForSale?> ExecuteAsync(int itemCode, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemCode);

        var number = itemCode.ToString(CultureInfo.InvariantCulture);
        var codes = Enumerable.Range(number.Length, Math.Max(0, MaxDigits - number.Length + 1))
            .Select(width => number.PadLeft(width, '0'))
            .ToList();

        var matches = (await products.FindByCodesAsync(codes, cancellationToken)).Where(p => p.Active).ToList();
        if (matches.Count > 1)
        {
            throw new DomainException(
                $"Os produtos {string.Join(", ", matches.Select(p => p.Code))} têm o mesmo código de balança ({itemCode}). Deixe só um ativo.");
        }

        return matches.Count == 0 ? null : await FindProductForSale.ToSaleAsync(matches[0], taxRules, cancellationToken);
    }
}

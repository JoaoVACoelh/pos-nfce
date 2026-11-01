using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Documents;

/// <summary>
/// Splits a discount on the whole sale into one discount per item (<c>vDesc</c> of each <c>det</c>), because the
/// NFC-e carries discounts per item and their sum must equal the total discount.
/// Each item gets its proportional share rounded down to cents; the leftover cents go, one by one, to the
/// items with the largest totals. No item ever gets a discount larger than its own total.
/// </summary>
public static class DiscountApportionment
{
    private const decimal Cent = 0.01m;

    public static IReadOnlyList<Money> Split(Money discount, IReadOnlyList<Money> itemTotals)
    {
        if (discount == Money.Zero)
        {
            return itemTotals.Select(_ => Money.Zero).ToList();
        }

        var subtotal = itemTotals.Aggregate(Money.Zero, (sum, total) => sum + total);
        if (discount.IsNegative || discount >= subtotal)
        {
            throw new DomainException("O desconto deve ser menor que o total dos itens.");
        }

        var shares = itemTotals
            .Select(total => decimal.Floor(discount.Amount * total.Amount / subtotal.Amount * 100m) / 100m)
            .ToArray();

        var leftover = discount.Amount - shares.Sum();
        var byLargest = Enumerable.Range(0, itemTotals.Count).OrderByDescending(i => itemTotals[i].Amount).ThenBy(i => i).ToList();

        while (leftover > 0)
        {
            var index = byLargest.First(i => shares[i] + Cent <= itemTotals[i].Amount);
            shares[index] += Cent;
            leftover -= Cent;
            byLargest.Remove(index);
            byLargest.Add(index); // next cent goes to the next item, round-robin
        }

        return shares.Select(share => new Money(share)).ToList();
    }
}

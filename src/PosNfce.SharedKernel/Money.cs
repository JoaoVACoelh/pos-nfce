using System.Globalization;

namespace PosNfce.SharedKernel;

/// <summary>
/// Amount in Brazilian reais. Wraps <see cref="decimal"/> so money is never handled as a floating-point number.
/// Rounding is not done here: each fiscal rule decides when and how to round.
/// </summary>
public readonly record struct Money(decimal Amount) : IComparable<Money>
{
    public static Money Zero { get; } = new(0m);

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator *(Money money, decimal factor) => new(money.Amount * factor);

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public bool IsNegative => Amount < 0m;

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    /// <summary>Brazilian format regardless of the machine's culture, e.g. "R$ 1.234,50".</summary>
    public override string ToString() => "R$ " + Amount.ToString("#,##0.00", BrazilianCulture);

    private static readonly CultureInfo BrazilianCulture = CultureInfo.GetCultureInfo("pt-BR");
}

using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Domain.Sales;

/// <summary>
/// How the customer paid. Cards go through a standalone terminal for now: the cashier types the amount there,
/// and here only records it. The Fiscal module maps each method to the NF-e <c>tPag</c> code.
/// </summary>
public enum PaymentMethod
{
    Cash,
    CreditCard,
    DebitCard,
    Pix,
}

public sealed record Payment
{
    public Guid Id { get; }
    public PaymentMethod Method { get; }
    public Money Amount { get; }

    /// <summary>True when charged through an integrated system (TEF); false for the standalone terminal and cash.</summary>
    public bool Integrated { get; }

    public Payment(Guid id, PaymentMethod method, Money amount, bool integrated)
    {
        if (!Enum.IsDefined(method))
        {
            throw new DomainException("Forma de pagamento inválida.");
        }

        if (amount <= Money.Zero)
        {
            throw new DomainException("O valor do pagamento deve ser maior que zero.");
        }

        if (decimal.Round(amount.Amount, 2) != amount.Amount)
        {
            throw new DomainException("O valor do pagamento tem no máximo 2 casas decimais.");
        }

        Id = id;
        Method = method;
        Amount = amount;
        Integrated = integrated;
    }
}

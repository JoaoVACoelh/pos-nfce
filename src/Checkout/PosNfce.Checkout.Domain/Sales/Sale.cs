using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Domain.Sales;

public enum SaleStatus
{
    Open,
    Completed,
    Cancelled,
}

/// <summary>
/// A sale at the checkout: items, an optional discount on the whole sale, and payments.
/// Only cash can exceed what is owed; the difference is the change.
/// </summary>
public sealed class Sale
{
    private readonly List<SaleItem> _items;
    private readonly List<Payment> _payments;

    public Guid Id { get; }
    public SaleStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Money Discount { get; private set; }

    /// <summary>"CPF na nota": optional customer identification sent in the NFC-e.</summary>
    public Cpf? CustomerCpf { get; private set; }

    public IReadOnlyList<SaleItem> Items => _items;
    public IReadOnlyList<Payment> Payments => _payments;

    public Money Subtotal => _items.Aggregate(Money.Zero, (sum, item) => sum + item.Total);
    public Money Total => Subtotal - Discount;
    public Money Paid => _payments.Aggregate(Money.Zero, (sum, p) => sum + p.Amount);

    /// <summary>What is still missing; zero once fully paid.</summary>
    public Money Remaining => Paid >= Total ? Money.Zero : Total - Paid;

    public Money Change => Paid > Total ? Paid - Total : Money.Zero;

    private Sale(Guid id, SaleStatus status, DateTimeOffset openedAt, DateTimeOffset? closedAt, Money discount,
        IEnumerable<SaleItem> items, IEnumerable<Payment> payments)
    {
        Id = id;
        Status = status;
        OpenedAt = openedAt;
        ClosedAt = closedAt;
        Discount = discount;
        _items = items.OrderBy(i => i.Line).ToList();
        _payments = payments.ToList();
    }

    public static Sale Open(DateTimeOffset now) => new(Guid.NewGuid(), SaleStatus.Open, now, null, Money.Zero, [], []);

    public static Sale Restore(Guid id, SaleStatus status, DateTimeOffset openedAt, DateTimeOffset? closedAt, Money discount,
        IEnumerable<SaleItem> items, IEnumerable<Payment> payments, Cpf? customerCpf = null) =>
        new(id, status, openedAt, closedAt, discount, items, payments) { CustomerCpf = customerCpf };

    /// <summary>Sets or clears (null) the customer's CPF. Allowed until the sale is closed.</summary>
    public void IdentifyCustomer(Cpf? cpf)
    {
        EnsureOpen();
        CustomerCpf = cpf;
    }

    public SaleItem AddItem(Guid productId, string code, string gtin, string description, string unit,
        Money unitPrice, decimal quantity, TaxSnapshot tax)
    {
        EnsureOpen();
        EnsureNoPayments("incluir itens");

        var line = _items.Count == 0 ? 1 : _items.Max(i => i.Line) + 1;
        var item = new SaleItem(Guid.NewGuid(), line, productId, code, gtin, description, unit, unitPrice, quantity, tax);
        _items.Add(item);
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureOpen();
        EnsureNoPayments("remover itens");

        var item = _items.SingleOrDefault(i => i.Id == itemId) ?? throw new DomainException("Item não encontrado na venda.");
        _items.Remove(item);

        if (Discount > Subtotal)
        {
            Discount = Money.Zero;
        }
    }

    public void ApplyDiscount(Money discount)
    {
        EnsureOpen();
        EnsureNoPayments("mudar o desconto");

        if (discount.IsNegative || decimal.Round(discount.Amount, 2) != discount.Amount)
        {
            throw new DomainException("Desconto inválido.");
        }

        // The NFC-e can't have a zero total: at least one cent must remain.
        if (_items.Count == 0 || discount >= Subtotal)
        {
            throw new DomainException("O desconto deve ser menor que o subtotal da venda.");
        }

        Discount = discount;
    }

    /// <summary>Checks a payment before charging it on the terminal, so a payment the sale would refuse is never charged.</summary>
    public void CheckPayment(PaymentMethod method, Money amount)
    {
        EnsureOpen();

        if (_items.Count == 0)
        {
            throw new DomainException("A venda não tem itens.");
        }

        if (Remaining == Money.Zero)
        {
            throw new DomainException("A venda já está paga.");
        }

        // Card and PIX are charged on the terminal for the exact amount: only cash gives change.
        if (method != PaymentMethod.Cash && amount > Remaining)
        {
            throw new DomainException($"Pagamento em cartão ou PIX não pode passar do que falta ({Remaining}).");
        }

        // Throwaway instance: runs the amount checks (positive, two decimals) that live in Payment.
        _ = new Payment(Guid.NewGuid(), method, amount, integrated: false);
    }

    public void AddPayment(PaymentMethod method, Money amount, bool integrated = false)
    {
        CheckPayment(method, amount);
        _payments.Add(new Payment(Guid.NewGuid(), method, amount, integrated));
    }

    public void RemovePayment(Guid paymentId)
    {
        EnsureOpen();
        var payment = _payments.SingleOrDefault(p => p.Id == paymentId) ?? throw new DomainException("Pagamento não encontrado.");
        _payments.Remove(payment);
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureOpen();

        if (_items.Count == 0)
        {
            throw new DomainException("A venda não tem itens.");
        }

        if (Remaining > Money.Zero)
        {
            throw new DomainException($"Ainda falta receber {Remaining}.");
        }

        Status = SaleStatus.Completed;
        ClosedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureOpen();
        Status = SaleStatus.Cancelled;
        ClosedAt = now;
    }

    private void EnsureOpen()
    {
        if (Status != SaleStatus.Open)
        {
            throw new DomainException("A venda já foi encerrada.");
        }
    }

    // Changing items after a payment would leave payments that no longer match the total.
    private void EnsureNoPayments(string action)
    {
        if (_payments.Count > 0)
        {
            throw new DomainException($"Remova os pagamentos antes de {action}.");
        }
    }
}

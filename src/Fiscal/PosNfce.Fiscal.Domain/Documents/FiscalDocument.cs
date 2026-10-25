using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Documents;

public enum FiscalDocumentStatus
{
    /// <summary>Sale received, NFC-e not issued yet.</summary>
    Pending,
    Authorized,
    Rejected,
    Contingency,
    Cancelled,
}

/// <summary>One sold line as the NFC-e will carry it: price, discount share and the tax data copied at the time of sale.</summary>
public sealed record FiscalItem(
    int Line,
    string Code,
    string Gtin,
    string Description,
    string Unit,
    decimal Quantity,
    Money UnitPrice,
    Money GrossTotal,
    Money Discount,
    string Ncm,
    string? Cest,
    int Origin,
    string Cfop,
    string Csosn,
    string PisCst,
    decimal PisRate,
    string CofinsCst,
    decimal CofinsRate,
    string IbsCbsCst,
    string ClassTrib,
    decimal IbsCbsReduction)
{
    public Money NetTotal => GrossTotal - Discount;
}

/// <summary>Payment as received by the checkout; mapped to the NF-e <c>tPag</c> code when the XML is built.</summary>
public sealed record FiscalPayment(string Method, Money Amount, bool Integrated);

/// <summary>
/// The NFC-e of one sale. Created as <see cref="FiscalDocumentStatus.Pending"/> when the sale is completed;
/// series and number are reserved only when it is about to be issued, so no number is wasted on a document never sent.
/// </summary>
public sealed class FiscalDocument
{
    private readonly List<FiscalItem> _items;
    private readonly List<FiscalPayment> _payments;

    public Guid Id { get; }
    public Guid SaleId { get; }
    public DateTimeOffset SoldAt { get; }
    public FiscalDocumentStatus Status { get; private set; }
    public int? Series { get; private set; }
    public long? Number { get; private set; }

    /// <summary>44-digit access key, known once the XML is signed.</summary>
    public string? AccessKey { get; private set; }
    public Money Discount { get; }
    public Money Change { get; }

    public IReadOnlyList<FiscalItem> Items => _items;
    public IReadOnlyList<FiscalPayment> Payments => _payments;

    public Money GrossTotal => _items.Aggregate(Money.Zero, (sum, i) => sum + i.GrossTotal);
    public Money Total => GrossTotal - Discount;

    private FiscalDocument(
        Guid id, Guid saleId, DateTimeOffset soldAt, FiscalDocumentStatus status, int? series, long? number,
        Money discount, Money change, IEnumerable<FiscalItem> items, IEnumerable<FiscalPayment> payments)
    {
        Id = id;
        SaleId = saleId;
        SoldAt = soldAt;
        Status = status;
        Series = series;
        Number = number;
        Discount = discount;
        Change = change;
        _items = items.OrderBy(i => i.Line).ToList();
        _payments = payments.ToList();
    }

    /// <summary>
    /// Builds the document from a completed sale, splitting the sale discount among the items.
    /// <paramref name="items"/> come without discount; it is filled here.
    /// </summary>
    public static FiscalDocument FromSale(
        Guid saleId, DateTimeOffset soldAt, IReadOnlyList<FiscalItem> items, Money discount, Money total, Money change,
        IReadOnlyList<FiscalPayment> payments)
    {
        if (items.Count == 0)
        {
            throw new DomainException("A venda não tem itens para a nota.");
        }

        var shares = DiscountApportionment.Split(discount, items.Select(i => i.GrossTotal).ToList());
        var withDiscount = items.Select((item, index) => item with { Discount = shares[index] }).ToList();

        var document = new FiscalDocument(
            Guid.NewGuid(), saleId, soldAt, FiscalDocumentStatus.Pending, null, null, discount, change, withDiscount, payments);

        // The sale and the document must agree to the cent; a difference means the sale data arrived broken.
        if (document.Total != total)
        {
            throw new DomainException($"Total da nota ({document.Total}) diferente do total da venda ({total}).");
        }

        var paid = payments.Aggregate(Money.Zero, (sum, p) => sum + p.Amount);
        if (paid - change != total)
        {
            throw new DomainException($"Pagamentos ({paid}) menos troco ({change}) não fecham com o total ({total}).");
        }

        return document;
    }

    public static FiscalDocument Restore(
        Guid id, Guid saleId, DateTimeOffset soldAt, FiscalDocumentStatus status, int? series, long? number,
        Money discount, Money change, IEnumerable<FiscalItem> items, IEnumerable<FiscalPayment> payments, string? accessKey = null) =>
        new(id, saleId, soldAt, status, series, number, discount, change, items, payments) { AccessKey = accessKey };

    /// <summary>Sets the series and number reserved for this document. Happens once, right before issuing.</summary>
    public void AssignNumber(int series, long number)
    {
        if (Status != FiscalDocumentStatus.Pending)
        {
            throw new DomainException("Só uma nota pendente recebe número.");
        }

        if (Number is not null)
        {
            throw new DomainException($"A nota já tem o número {Number}.");
        }

        (Series, Number) = (series, number);
    }

    /// <summary>Records the access key of the signed XML. The document stays pending until SEFAZ answers.</summary>
    public void RecordSignature(string accessKey)
    {
        if (accessKey.Length != 44 || !accessKey.All(char.IsAsciiDigit))
        {
            throw new DomainException("Chave de acesso deve ter 44 dígitos.");
        }

        if (Status != FiscalDocumentStatus.Pending || Number is null)
        {
            throw new DomainException("Só uma nota pendente e numerada pode ser assinada.");
        }

        AccessKey = accessKey;
    }
}

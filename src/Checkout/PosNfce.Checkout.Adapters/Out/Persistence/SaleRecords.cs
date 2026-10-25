namespace PosNfce.Checkout.Adapters.Out.Persistence;

/// <summary>Row of <c>checkout_sales</c>.</summary>
public sealed class SaleRecord
{
    public Guid Id { get; set; }
    public int Status { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public List<SaleItemRecord> Items { get; set; } = [];
    public List<PaymentRecord> Payments { get; set; } = [];
}

/// <summary>Row of <c>checkout_sale_items</c>, with the tax data copied at the time of sale.</summary>
public sealed class SaleItemRecord
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public int Line { get; set; }
    public Guid ProductId { get; set; }
    public string Code { get; set; } = "";
    public string Gtin { get; set; } = "";
    public string Description { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal Total { get; set; }
    public string Ncm { get; set; } = "";
    public string? Cest { get; set; }
    public int Origin { get; set; }
    public string Cfop { get; set; } = "";
    public string Csosn { get; set; } = "";
    public string PisCst { get; set; } = "";
    public decimal PisRate { get; set; }
    public string CofinsCst { get; set; } = "";
    public decimal CofinsRate { get; set; }
    public string IbsCbsCst { get; set; } = "";
    public string ClassTrib { get; set; } = "";
}

/// <summary>Row of <c>checkout_payments</c>.</summary>
public sealed class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public int Method { get; set; }
    public decimal Amount { get; set; }
    public bool Integrated { get; set; }
}

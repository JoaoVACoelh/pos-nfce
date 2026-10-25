namespace PosNfce.Fiscal.Adapters.Out.Persistence;

/// <summary>Row of <c>fiscal_documents</c>.</summary>
public sealed class FiscalDocumentRecord
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public DateTimeOffset SoldAt { get; set; }
    public int Status { get; set; }
    public int? Series { get; set; }
    public long? Number { get; set; }
    public string? AccessKey { get; set; }
    public string? CustomerCpf { get; set; }
    public string? Protocol { get; set; }
    public DateTimeOffset? AuthorizedAt { get; set; }
    public int? LastStatusCode { get; set; }
    public string? LastMessage { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal Change { get; set; }
    public List<FiscalItemRecord> Items { get; set; } = [];
    public List<FiscalPaymentRecord> Payments { get; set; } = [];
}

/// <summary>Row of <c>fiscal_document_items</c>.</summary>
public sealed class FiscalItemRecord
{
    public int Id { get; set; }
    public Guid DocumentId { get; set; }
    public int Line { get; set; }
    public string Code { get; set; } = "";
    public string Gtin { get; set; } = "";
    public string Description { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal GrossTotal { get; set; }
    public decimal Discount { get; set; }
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
    public decimal IbsCbsReduction { get; set; }
}

/// <summary>Row of <c>fiscal_document_payments</c>.</summary>
public sealed class FiscalPaymentRecord
{
    public int Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Method { get; set; } = "";
    public decimal Amount { get; set; }
    public bool Integrated { get; set; }
}

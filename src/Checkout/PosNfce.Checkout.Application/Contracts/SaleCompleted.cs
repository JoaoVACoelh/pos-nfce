using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Application.Contracts;

/// <summary>
/// Published when a sale is paid and closed. It carries everything needed to issue the NFC-e, so the Fiscal
/// module never reads Checkout's tables. Plain types only: it is a contract between modules.
/// </summary>
public sealed record SaleCompleted(
    Guid SaleId,
    DateTimeOffset OccurredAt,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    decimal Change,
    IReadOnlyList<SoldItem> Items,
    IReadOnlyList<ReceivedPayment> Payments) : IDomainEvent;

public sealed record SoldItem(
    int Line,
    Guid ProductId,
    string Code,
    string Gtin,
    string Description,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total,
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
    decimal IbsCbsReduction);

/// <summary>
/// Method is the name of Checkout's PaymentMethod (Cash, CreditCard, DebitCard, Pix).
/// Integrated: charged through TEF (true) or a standalone terminal / cash (false).
/// </summary>
public sealed record ReceivedPayment(string Method, decimal Amount, bool Integrated);

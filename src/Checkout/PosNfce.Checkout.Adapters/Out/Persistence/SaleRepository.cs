using Microsoft.EntityFrameworkCore;
using PosNfce.Checkout.Application.Ports;
using PosNfce.Checkout.Domain.Sales;
using PosNfce.SharedKernel;

namespace PosNfce.Checkout.Adapters.Out.Persistence;

public sealed class SaleRepository(CheckoutDbContext db) : ISaleRepository
{
    public async Task<Sale?> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        var record = await db.Sales.AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Payments)
            .Where(s => s.Status == (int)SaleStatus.Open)
            .SingleOrDefaultAsync(cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    /// <summary>The aggregate is small: delete and re-insert it, so removed items and payments disappear too.</summary>
    public async Task SaveAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Sales.Where(s => s.Id == sale.Id).ExecuteDeleteAsync(cancellationToken);
        db.Sales.Add(ToRecord(sale));
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static Sale ToDomain(SaleRecord r) => Sale.Restore(
        r.Id, (SaleStatus)r.Status, r.OpenedAt, r.ClosedAt, new Money(r.Discount),
        r.Items.Select(i => new SaleItem(
            i.Id, i.Line, i.ProductId, i.Code, i.Gtin, i.Description, i.Unit, new Money(i.UnitPrice), i.Quantity,
            new TaxSnapshot(i.Ncm, i.Cest, i.Origin, i.Cfop, i.Csosn, i.PisCst, i.PisRate, i.CofinsCst, i.CofinsRate, i.IbsCbsCst, i.ClassTrib, i.IbsCbsReduction))),
        r.Payments.Select(p => new Payment(p.Id, (PaymentMethod)p.Method, new Money(p.Amount), p.Integrated)),
        r.CustomerCpf is null ? null : Cpf.Parse(r.CustomerCpf));

    private static SaleRecord ToRecord(Sale sale) => new()
    {
        Id = sale.Id,
        Status = (int)sale.Status,
        OpenedAt = sale.OpenedAt,
        ClosedAt = sale.ClosedAt,
        Discount = sale.Discount.Amount,
        Total = sale.Total.Amount,
        CustomerCpf = sale.CustomerCpf?.Value,
        Items = sale.Items.Select(i => new SaleItemRecord
        {
            Id = i.Id,
            SaleId = sale.Id,
            Line = i.Line,
            ProductId = i.ProductId,
            Code = i.Code,
            Gtin = i.Gtin,
            Description = i.Description,
            Unit = i.Unit,
            UnitPrice = i.UnitPrice.Amount,
            Quantity = i.Quantity,
            Total = i.Total.Amount,
            Ncm = i.Tax.Ncm,
            Cest = i.Tax.Cest,
            Origin = i.Tax.Origin,
            Cfop = i.Tax.Cfop,
            Csosn = i.Tax.Csosn,
            PisCst = i.Tax.PisCst,
            PisRate = i.Tax.PisRate,
            CofinsCst = i.Tax.CofinsCst,
            CofinsRate = i.Tax.CofinsRate,
            IbsCbsCst = i.Tax.IbsCbsCst,
            ClassTrib = i.Tax.ClassTrib,
            IbsCbsReduction = i.Tax.IbsCbsReduction,
        }).ToList(),
        Payments = sale.Payments.Select(p => new PaymentRecord
        {
            Id = p.Id,
            SaleId = sale.Id,
            Method = (int)p.Method,
            Amount = p.Amount.Amount,
            Integrated = p.Integrated,
        }).ToList(),
    };
}

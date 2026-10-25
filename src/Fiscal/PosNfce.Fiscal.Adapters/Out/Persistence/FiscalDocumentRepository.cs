using Microsoft.EntityFrameworkCore;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Adapters.Out.Persistence;

public sealed class FiscalDocumentRepository(FiscalDbContext db) : IFiscalDocumentRepository
{
    public Task<bool> ExistsForSaleAsync(Guid saleId, CancellationToken cancellationToken = default) =>
        db.Documents.AnyAsync(d => d.SaleId == saleId, cancellationToken);

    public async Task AddAsync(FiscalDocument document, CancellationToken cancellationToken = default)
    {
        db.Documents.Add(ToRecord(document));
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    public async Task<FiscalDocument?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = await db.Documents.AsNoTracking().Include(d => d.Items).Include(d => d.Payments)
            .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        return record is null ? null : ToDomain(record);
    }

    public async Task UpdateAsync(FiscalDocument document, CancellationToken cancellationToken = default)
    {
        await db.Documents.Where(d => d.Id == document.Id).ExecuteUpdateAsync(set => set
            .SetProperty(d => d.Status, (int)document.Status)
            .SetProperty(d => d.Series, document.Series)
            .SetProperty(d => d.Number, document.Number)
            .SetProperty(d => d.AccessKey, document.AccessKey)
            .SetProperty(d => d.Protocol, document.Protocol)
            .SetProperty(d => d.AuthorizedAt, document.AuthorizedAt)
            .SetProperty(d => d.LastStatusCode, document.LastStatusCode)
            .SetProperty(d => d.LastMessage, document.LastMessage)
            .SetProperty(d => d.QrCode, document.QrCode)
            .SetProperty(d => d.ConsultUrl, document.ConsultUrl)
            .SetProperty(d => d.AdditionalInfo, document.AdditionalInfo), cancellationToken);
    }

    public async Task<IReadOnlyList<FiscalDocument>> ListRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        // Ordered in memory: SQLite stores DateTimeOffset as text, which EF can't order by in SQL.
        var records = await db.Documents.AsNoTracking().Include(d => d.Items).Include(d => d.Payments).ToListAsync(cancellationToken);
        return records.OrderByDescending(d => d.SoldAt).Take(limit).Select(ToDomain).ToList();
    }

    internal static FiscalDocument ToDomain(FiscalDocumentRecord r) => FiscalDocument.Restore(
        r.Id, r.SaleId, r.SoldAt, (FiscalDocumentStatus)r.Status, r.Series, r.Number,
        new Money(r.Discount), new Money(r.Change),
        r.Items.Select(i => new FiscalItem(
            i.Line, i.Code, i.Gtin, i.Description, i.Unit, i.Quantity, new Money(i.UnitPrice), new Money(i.GrossTotal), new Money(i.Discount),
            i.Ncm, i.Cest, i.Origin, i.Cfop, i.Csosn, i.PisCst, i.PisRate, i.CofinsCst, i.CofinsRate, i.IbsCbsCst, i.ClassTrib, i.IbsCbsReduction)),
        r.Payments.Select(p => new FiscalPayment(p.Method, new Money(p.Amount), p.Integrated)),
        r.AccessKey, r.CustomerCpf, r.Protocol, r.AuthorizedAt, r.LastStatusCode, r.LastMessage, r.QrCode, r.ConsultUrl, r.AdditionalInfo);

    private static FiscalDocumentRecord ToRecord(FiscalDocument d) => new()
    {
        Id = d.Id,
        SaleId = d.SaleId,
        SoldAt = d.SoldAt,
        Status = (int)d.Status,
        Series = d.Series,
        Number = d.Number,
        AccessKey = d.AccessKey,
        CustomerCpf = d.CustomerCpf,
        Protocol = d.Protocol,
        AuthorizedAt = d.AuthorizedAt,
        LastStatusCode = d.LastStatusCode,
        LastMessage = d.LastMessage,
        QrCode = d.QrCode,
        ConsultUrl = d.ConsultUrl,
        AdditionalInfo = d.AdditionalInfo,
        Discount = d.Discount.Amount,
        Total = d.Total.Amount,
        Change = d.Change.Amount,
        Items = d.Items.Select(i => new FiscalItemRecord
        {
            DocumentId = d.Id,
            Line = i.Line,
            Code = i.Code,
            Gtin = i.Gtin,
            Description = i.Description,
            Unit = i.Unit,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice.Amount,
            GrossTotal = i.GrossTotal.Amount,
            Discount = i.Discount.Amount,
            Ncm = i.Ncm,
            Cest = i.Cest,
            Origin = i.Origin,
            Cfop = i.Cfop,
            Csosn = i.Csosn,
            PisCst = i.PisCst,
            PisRate = i.PisRate,
            CofinsCst = i.CofinsCst,
            CofinsRate = i.CofinsRate,
            IbsCbsCst = i.IbsCbsCst,
            ClassTrib = i.ClassTrib,
            IbsCbsReduction = i.IbsCbsReduction,
        }).ToList(),
        Payments = d.Payments.Select(p => new FiscalPaymentRecord
        {
            DocumentId = d.Id,
            Method = p.Method,
            Amount = p.Amount.Amount,
            Integrated = p.Integrated,
        }).ToList(),
    };
}

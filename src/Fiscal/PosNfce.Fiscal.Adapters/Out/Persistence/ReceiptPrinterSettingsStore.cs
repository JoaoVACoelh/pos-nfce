using Microsoft.EntityFrameworkCore;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Application.Printing;

namespace PosNfce.Fiscal.Adapters.Out.Persistence;

/// <summary>Row of <c>fiscal_receipt_printer</c>: a single row (Id 1) per installation.</summary>
public sealed class ReceiptPrinterRecord
{
    public const int SingleId = 1;

    public int Id { get; set; } = SingleId;
    public int Connection { get; set; }
    public string? PrinterName { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; }
    public int Columns { get; set; }
    public int CodePage { get; set; }
    public int QrCode { get; set; }
    public bool CutPaper { get; set; }
    public bool PrintAfterAuthorization { get; set; }
}

public sealed class ReceiptPrinterSettingsStore(FiscalDbContext db) : IReceiptPrinterSettingsStore
{
    public async Task<ReceiptPrinterSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var r = await db.ReceiptPrinter.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return r is null
            ? ReceiptPrinterSettings.Default
            : new ReceiptPrinterSettings(
                (PrinterConnection)r.Connection, r.PrinterName, r.Host, r.Port, r.Columns, (PrinterCodePage)r.CodePage,
                (QrCodeMode)r.QrCode, r.CutPaper, r.PrintAfterAuthorization);
    }

    public async Task SaveAsync(ReceiptPrinterSettings settings, CancellationToken cancellationToken = default)
    {
        var record = new ReceiptPrinterRecord
        {
            Connection = (int)settings.Connection,
            PrinterName = settings.PrinterName,
            Host = settings.Host,
            Port = settings.Port,
            Columns = settings.Columns,
            CodePage = (int)settings.CodePage,
            QrCode = (int)settings.QrCode,
            CutPaper = settings.CutPaper,
            PrintAfterAuthorization = settings.PrintAfterAuthorization,
        };

        if (await db.ReceiptPrinter.AnyAsync(cancellationToken))
        {
            db.ReceiptPrinter.Update(record);
        }
        else
        {
            db.ReceiptPrinter.Add(record);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

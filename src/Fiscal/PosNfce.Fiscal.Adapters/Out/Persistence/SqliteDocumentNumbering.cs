using Microsoft.EntityFrameworkCore;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Adapters.Out.Persistence;

/// <summary>
/// Takes the issuer's next number and gives it to the document in one write transaction.
/// Microsoft.Data.Sqlite opens transactions with <c>BEGIN IMMEDIATE</c>, which takes SQLite's write lock at the
/// start: a second reservation waits until the first commits, so it always reads the already incremented number.
/// The unique index on (series, number) is the last line of defense.
/// </summary>
public sealed class SqliteDocumentNumbering(FiscalDbContext db) : IDocumentNumbering
{
    public async Task<DocumentNumber> ReserveAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var issuer = await db.Issuers.SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("Cadastre o emitente antes de emitir notas.");
        var record = await db.Documents.Include(d => d.Items).Include(d => d.Payments)
            .SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken)
            ?? throw new DomainException("Nota não encontrada.");

        // The domain checks that the document is pending and has no number yet.
        var document = FiscalDocumentRepository.ToDomain(record);
        document.AssignNumber(issuer.Series, issuer.NextNumber);

        record.Series = document.Series;
        record.Number = document.Number;
        issuer.NextNumber++;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.ChangeTracker.Clear();

        return new DocumentNumber(document.Series!.Value, document.Number!.Value);
    }
}

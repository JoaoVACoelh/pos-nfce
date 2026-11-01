namespace PosNfce.Fiscal.Application.Ports;

/// <summary>
/// Reserves the next NFC-e number of the issuer's series for a pending document. Implementations must do it in a
/// single write transaction, so two documents never get the same number, even with two operations at once.
/// </summary>
public interface IDocumentNumbering
{
    Task<DocumentNumber> ReserveAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public sealed record DocumentNumber(int Series, long Number);

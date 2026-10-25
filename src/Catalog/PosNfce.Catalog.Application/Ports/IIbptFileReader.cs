using PosNfce.Catalog.Domain.Ibpt;

namespace PosNfce.Catalog.Application.Ports;

/// <summary>
/// Reads the IBPT file published by IBPT. Only NCM rows (goods) are returned; service rows (NBS, LC 116)
/// and codes that aren't a full 8-digit NCM are skipped and counted.
/// </summary>
public interface IIbptFileReader
{
    /// <summary>Throws <see cref="SharedKernel.DomainException"/> naming the line when the file doesn't follow the IBPT layout.</summary>
    Task<IbptFile> ReadAsync(Stream file, CancellationToken cancellationToken = default);
}

public sealed record IbptFile(IReadOnlyList<IbptRate> Rates, int SkippedRows);

using PosNfce.Catalog.Application.Ports;
using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Application.Ibpt;

public sealed record IbptImportResult(int Imported, int SkippedRows, string Version, DateOnly ValidFrom, DateOnly ValidUntil);

public sealed class ImportIbptTable(IIbptFileReader reader, IIbptRateRepository rates)
{
    public async Task<IbptImportResult> ExecuteAsync(Stream file, CancellationToken cancellationToken = default)
    {
        var table = await reader.ReadAsync(file, cancellationToken);

        if (table.Rates.Count == 0)
        {
            throw new DomainException("O arquivo não tem nenhuma linha de NCM. Confira se é a tabela IBPT de produtos.");
        }

        var duplicate = table.Rates.GroupBy(r => (r.Ncm.Value, r.Ex)).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new DomainException($"O NCM {duplicate.Key.Value} aparece mais de uma vez no arquivo.");
        }

        await rates.ReplaceAllAsync(table.Rates, cancellationToken);

        return new IbptImportResult(
            table.Rates.Count,
            table.SkippedRows,
            table.Rates[0].Version,
            table.Rates.Min(r => r.ValidFrom),
            table.Rates.Max(r => r.ValidUntil));
    }
}

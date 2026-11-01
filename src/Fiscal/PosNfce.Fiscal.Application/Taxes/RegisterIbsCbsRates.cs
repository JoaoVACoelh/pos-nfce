using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Taxes;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Taxes;

public sealed record RegisterIbsCbsRatesCommand(DateOnly ValidFrom, decimal IbsState, decimal IbsMunicipal, decimal Cbs, string Source);

/// <summary>Registers the IBS/CBS rates of a new period. Periods are never edited, so past documents keep their rates.</summary>
public sealed class RegisterIbsCbsRates(IIbsCbsRateRepository rates)
{
    public async Task ExecuteAsync(RegisterIbsCbsRatesCommand command, CancellationToken cancellationToken = default)
    {
        var period = new IbsCbsRates(command.ValidFrom, command.IbsState, command.IbsMunicipal, command.Cbs, command.Source);

        if (await rates.ExistsFromAsync(period.ValidFrom, cancellationToken))
        {
            throw new DomainException($"Já existem alíquotas de IBS/CBS a partir de {period.ValidFrom:dd/MM/yyyy}.");
        }

        await rates.AddAsync(period, cancellationToken);
    }
}

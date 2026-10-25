namespace PosNfce.SharedKernel;

/// <summary>
/// Current time in America/Sao_Paulo, whatever the machine's time zone. Fiscal documents carry date and time
/// with this offset, so a store PC set to another zone must not change them.
/// </summary>
public static class BrazilTime
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static DateTimeOffset Now(TimeProvider clock) => TimeZoneInfo.ConvertTime(clock.GetUtcNow(), SaoPaulo);
}

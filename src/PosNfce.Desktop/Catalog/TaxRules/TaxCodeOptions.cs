using PosNfce.Desktop.Shared;

namespace PosNfce.Desktop.Catalog.TaxRules;

/// <summary>
/// Official code tables (NF-e layout) behind the tax rule drop-downs, with their names shortened for the screen.
/// They only list what exists: which code each product uses still comes from the accountant.
/// </summary>
public static class TaxCodeOptions
{
    /// <summary>Closed list: the same codes <c>Csosn</c> accepts in the domain.</summary>
    public static IReadOnlyList<CodeOption> Csosn { get; } =
    [
        new("101", "Tributada com permissão de crédito"),
        new("102", "Tributada sem permissão de crédito"),
        new("103", "Isenção do ICMS para faixa de receita bruta"),
        new("201", "Com permissão de crédito e cobrança do ICMS por ST"),
        new("202", "Sem permissão de crédito e com cobrança do ICMS por ST"),
        new("203", "Isenção para faixa de receita bruta e cobrança do ICMS por ST"),
        new("300", "Imune"),
        new("400", "Não tributada"),
        new("500", "ICMS cobrado anteriormente por ST ou por antecipação"),
        new("900", "Outros"),
    ];

    /// <summary>
    /// Closed list with the output codes only: an NFC-e is always a sale. The domain also accepts the input codes
    /// (50 to 98), which belong to purchases.
    /// </summary>
    public static IReadOnlyList<CodeOption> PisCofinsCst { get; } =
    [
        new("01", "Tributável com alíquota básica"),
        new("02", "Tributável com alíquota diferenciada"),
        new("03", "Tributável com alíquota por unidade de medida"),
        new("04", "Tributável monofásica, revenda a alíquota zero"),
        new("05", "Tributável por substituição tributária"),
        new("06", "Tributável a alíquota zero"),
        new("07", "Isenta da contribuição"),
        new("08", "Sem incidência da contribuição"),
        new("09", "Com suspensão da contribuição"),
        new("49", "Outras operações de saída"),
        new("99", "Outras operações"),
    ];

    /// <summary>Suggestions only: any 5.xxx CFOP the accountant gives can be typed.</summary>
    public static IReadOnlyList<CodeOption> Cfop { get; } =
    [
        new("5101", "Venda de produção do estabelecimento"),
        new("5102", "Venda de mercadoria de terceiros"),
        new("5401", "Venda de produção, com ST, como contribuinte substituto"),
        new("5403", "Venda de mercadoria de terceiros, com ST, como contribuinte substituto"),
        new("5405", "Venda de mercadoria de terceiros, com ST, como contribuinte substituído"),
    ];

    /// <summary>Suggestions only: the codes the NFC-e issuing supports today. The official table is still changing.</summary>
    public static IReadOnlyList<CodeOption> IbsCbsCst { get; } =
    [
        new("000", "Tributação integral"),
        new("200", "Alíquota reduzida"),
        new("400", "Isenção"),
        new("410", "Imunidade e não incidência"),
    ];
}

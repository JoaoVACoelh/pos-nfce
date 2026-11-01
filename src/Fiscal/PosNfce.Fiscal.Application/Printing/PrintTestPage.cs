using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Ports;

namespace PosNfce.Fiscal.Application.Printing;

/// <summary>
/// Prints a test receipt with the settings on the screen (saved or not): a column ruler to check the paper width,
/// accented letters to check the character table, and a QR Code to check the QR mode.
/// </summary>
public sealed class PrintTestPage(IReceiptPrinter printer)
{
    /// <summary>Any valid URL works; this is SEFAZ-MG's NFC-e portal.</summary>
    private const string SampleQrCode = "https://portalsped.fazenda.mg.gov.br/portalnfce";

    public async Task ExecuteAsync(ReceiptPrinterSettings settings, CancellationToken cancellationToken = default)
    {
        var valid = settings.Validated();
        var width = valid.Columns;
        var lines = new List<DanfceLine>();
        void Add(string text, DanfceStyle style = DanfceStyle.Normal) =>
            lines.AddRange(DanfceLayout.Wrap(text, width).Select(part => new DanfceLine(part, style)));
        void Rule() => lines.Add(new DanfceLine(new string('-', width), DanfceStyle.Rule));

        Add("TESTE DE IMPRESSÃO", DanfceStyle.CenteredBold);
        Add("POS NFC-e", DanfceStyle.Centered);
        Rule();
        Add($"Largura: {width} colunas. A régua abaixo deve ocupar uma linha só, sem quebrar:");
        lines.Add(new DanfceLine(string.Concat(Enumerable.Range(1, width).Select(i => (char)('0' + i % 10)))));
        Rule();
        Add("Acentos: ÁÉÍÓÚ áéíóú ÃÕ ãõ Ç ç Ê ê nº");
        Add("Se aparecerem símbolos estranhos, troque a tabela de caracteres ou use \"Sem acentos\".");
        Rule();
        Add("Negrito", DanfceStyle.Bold);
        Add("Centralizado", DanfceStyle.Centered);
        Add("QR Code (deve abrir o portal da SEFAZ-MG):", DanfceStyle.Small);
        lines.Add(new DanfceLine(Danfce.QrCodePlaceholder, DanfceStyle.Centered));
        Add("FIM DO TESTE", DanfceStyle.CenteredBold);

        await printer.PrintAsync(new Danfce(lines, SampleQrCode, width, Authorized: false), valid, cancellationToken);
    }
}

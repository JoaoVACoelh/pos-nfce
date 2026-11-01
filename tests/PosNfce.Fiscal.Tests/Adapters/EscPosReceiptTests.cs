using PosNfce.Fiscal.Adapters.Out.Printing;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Printing;

namespace PosNfce.Fiscal.Tests.Adapters;

/// <summary>The bytes sent to the printer. Only standard ESC/POS commands, so any brand prints the same receipt.</summary>
public sealed class EscPosReceiptTests
{
    private const byte Esc = 0x1B;
    private const byte Gs = 0x1D;

    private static readonly ReceiptPrinterSettings Settings = ReceiptPrinterSettings.Default with { PrinterName = "Termica" };

    private static Danfce Receipt(params DanfceLine[] lines) =>
        new(lines, "https://portalsped.fazenda.mg.gov.br/portalnfce?p=31261011222333000181650010000000011080128250|3|2|1|ABC", 48, false);

    private static bool Contains(byte[] bytes, params byte[] sequence) =>
        Enumerable.Range(0, bytes.Length - sequence.Length + 1).Any(i => bytes.AsSpan(i, sequence.Length).SequenceEqual(sequence));

    [Fact]
    public void Starts_by_resetting_the_printer()
    {
        var bytes = EscPosReceipt.Encode(Receipt(new DanfceLine("A")), Settings);

        Assert.Equal(new byte[] { Esc, (byte)'@' }, bytes[..2]);
    }

    [Fact]
    public void Without_accents_the_text_is_plain_ascii_and_no_table_is_selected()
    {
        var bytes = EscPosReceipt.Encode(Receipt(new DanfceLine("Eletrônica nº ção")), Settings);

        Assert.True(Contains(bytes, "Eletronica no cao"u8.ToArray()));
        Assert.False(Contains(bytes, Esc, (byte)'t'));
        Assert.All(bytes, b => Assert.True(b < 128));
    }

    [Fact]
    public void Pc850_selects_table_2_and_encodes_accents_with_it()
    {
        var bytes = EscPosReceipt.Encode(Receipt(new DanfceLine("é ã ç")), Settings with { CodePage = PrinterCodePage.Pc850 });

        Assert.True(Contains(bytes, Esc, (byte)'t', 2));
        Assert.True(Contains(bytes, 0x82, 0x20, 0xC6, 0x20, 0x87)); // é ã ç in code page 850
    }

    [Fact]
    public void Centered_and_bold_lines_switch_the_printer_styles()
    {
        var bytes = EscPosReceipt.Encode(Receipt(new DanfceLine("TOTAL", DanfceStyle.CenteredBold)), Settings);

        Assert.True(Contains(bytes, Esc, (byte)'a', 1, Esc, (byte)'E', 1, (byte)'T', (byte)'O', (byte)'T', (byte)'A', (byte)'L', 0x0A));
    }

    [Fact]
    public void Native_qr_code_stores_the_whole_content_and_prints_it()
    {
        var receipt = Receipt(new DanfceLine(Danfce.QrCodePlaceholder, DanfceStyle.Centered));
        var bytes = EscPosReceipt.Encode(receipt, Settings);

        var length = receipt.QrCode.Length + 3;
        Assert.True(Contains(bytes, Gs, (byte)'(', (byte)'k', (byte)(length % 256), (byte)(length / 256), 49, 80, 48));
        Assert.True(Contains(bytes, System.Text.Encoding.ASCII.GetBytes(receipt.QrCode)));
        Assert.True(Contains(bytes, Gs, (byte)'(', (byte)'k', 3, 0, 49, 81, 48));
        Assert.False(Contains(bytes, Gs, (byte)'v', (byte)'0'));
    }

    [Fact]
    public void Image_qr_code_is_a_square_bitmap_for_printers_without_the_qr_command()
    {
        var bytes = EscPosReceipt.Encode(
            Receipt(new DanfceLine(Danfce.QrCodePlaceholder, DanfceStyle.Centered)), Settings with { QrCode = QrCodeMode.Image });

        var start = Enumerable.Range(0, bytes.Length - 3).First(i => bytes[i] == Gs && bytes[i + 1] == 'v' && bytes[i + 2] == '0');
        var bytesPerRow = bytes[start + 4] + bytes[start + 5] * 256;
        var rows = bytes[start + 6] + bytes[start + 7] * 256;
        Assert.Equal((rows + 7) / 8, bytesPerRow);
        Assert.True(rows * 1 <= 576, "fits the 576 dots of an 80 mm head");
        Assert.True(bytes.Length >= start + 8 + bytesPerRow * rows);
        Assert.False(Contains(bytes, Gs, (byte)'(', (byte)'k'));
    }

    [Fact]
    public void Paper_is_cut_only_when_asked()
    {
        var receipt = Receipt(new DanfceLine("A"));

        Assert.Equal(new byte[] { Gs, (byte)'V', 1 }, EscPosReceipt.Encode(receipt, Settings)[^3..]);
        Assert.False(Contains(EscPosReceipt.Encode(receipt, Settings with { CutPaper = false }), Gs, (byte)'V'));
    }
}

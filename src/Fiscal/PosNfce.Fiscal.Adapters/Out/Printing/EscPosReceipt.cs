using System.Globalization;
using System.Text;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Printing;
using QRCoder;

namespace PosNfce.Fiscal.Adapters.Out.Printing;

/// <summary>
/// Turns receipt lines into ESC/POS bytes. Only the basic commands of the Epson ESC/POS set are used (initialize,
/// alignment, bold, character table, QR Code, raster image, cut), which the brands sold in Brazil implement the same way.
/// Nothing here depends on one printer model.
/// </summary>
public static class EscPosReceipt
{
    private const byte Esc = 0x1B;
    private const byte Gs = 0x1D;
    private const byte LineFeed = 0x0A;

    public static byte[] Encode(Danfce receipt, ReceiptPrinterSettings settings)
    {
        var output = new List<byte>();
        void Write(params byte[] bytes) => output.AddRange(bytes);

        Write(Esc, (byte)'@'); // initialize: clears styles left by a previous job
        if (TableNumber(settings.CodePage) is { } table)
        {
            Write(Esc, (byte)'t', table);
        }

        var encoding = TextEncoding(settings.CodePage);
        foreach (var line in receipt.Lines)
        {
            var centered = line.Style is DanfceStyle.Centered or DanfceStyle.CenteredBold;
            Write(Esc, (byte)'a', centered ? (byte)1 : (byte)0);

            if (line.Text == Danfce.QrCodePlaceholder)
            {
                if (receipt.QrCode.Length > 0)
                {
                    Write(LineFeed);
                    output.AddRange(settings.QrCode == QrCodeMode.Native
                        ? NativeQrCode(receipt.QrCode, receipt.Width)
                        : RasterQrCode(receipt.QrCode, receipt.Width));
                    Write(LineFeed);
                }

                continue;
            }

            var bold = line.Style is DanfceStyle.Bold or DanfceStyle.CenteredBold;
            Write(Esc, (byte)'E', bold ? (byte)1 : (byte)0);
            output.AddRange(encoding.GetBytes(settings.CodePage == PrinterCodePage.NoAccents ? RemoveAccents(line.Text) : line.Text));
            Write(LineFeed);
        }

        Write(Esc, (byte)'E', 0);
        Write(Esc, (byte)'a', 0);
        Write(Esc, (byte)'d', 4); // feed past the cutter
        if (settings.CutPaper)
        {
            Write(Gs, (byte)'V', 1); // partial cut; printers without a cutter ignore it
        }

        return [.. output];
    }

    /// <summary>"ESC t n" numbers of the Epson table, followed by the other ESC/POS brands.</summary>
    private static byte? TableNumber(PrinterCodePage codePage) => codePage switch
    {
        PrinterCodePage.Pc850 => 2,
        PrinterCodePage.Pc860 => 3,
        PrinterCodePage.Windows1252 => 16,
        _ => null,
    };

    private static Encoding TextEncoding(PrinterCodePage codePage) => codePage switch
    {
        PrinterCodePage.Pc850 => CodePagesEncodingProvider.Instance.GetEncoding(850)!,
        PrinterCodePage.Pc860 => CodePagesEncodingProvider.Instance.GetEncoding(860)!,
        PrinterCodePage.Windows1252 => CodePagesEncodingProvider.Instance.GetEncoding(1252)!,
        _ => Encoding.ASCII,
    };

    /// <summary>"Eletrônica nº" → "Eletronica no". Anything still outside ASCII becomes "?".</summary>
    internal static string RemoveAccents(string text)
    {
        var plain = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            plain.Append(c switch
            {
                'º' => 'o',
                'ª' => 'a',
                < (char)128 => c,
                _ => '?',
            });
        }

        return plain.ToString();
    }

    /// <summary>QR Code drawn by the printer: model 2, error correction M, module size by paper width ("GS ( k").</summary>
    private static byte[] NativeQrCode(string content, int columns)
    {
        var data = Encoding.ASCII.GetBytes(content);
        var length = data.Length + 3;
        var moduleSize = columns >= 42 ? (byte)5 : (byte)4;

        return
        [
            Gs, (byte)'(', (byte)'k', 4, 0, 49, 65, 50, 0,        // model 2
            Gs, (byte)'(', (byte)'k', 3, 0, 49, 67, moduleSize,   // module size in dots
            Gs, (byte)'(', (byte)'k', 3, 0, 49, 69, 49,           // error correction level M
            Gs, (byte)'(', (byte)'k', (byte)(length % 256), (byte)(length / 256), 49, 80, 48, .. data, // store the content
            Gs, (byte)'(', (byte)'k', 3, 0, 49, 81, 48,           // print it
        ];
    }

    /// <summary>QR Code sent as a black and white bitmap ("GS v 0"), for printers without the QR command.</summary>
    private static byte[] RasterQrCode(string content, int columns)
    {
        using var generator = new QRCodeGenerator();
        using var qr = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        var matrix = qr.ModuleMatrix; // includes the quiet zone
        var scale = columns >= 42 ? 6 : 4;
        var size = matrix.Count * scale;
        var bytesPerRow = (size + 7) / 8;

        var image = new List<byte> { Gs, (byte)'v', (byte)'0', 0, (byte)(bytesPerRow % 256), (byte)(bytesPerRow / 256), (byte)(size % 256), (byte)(size / 256) };
        for (var y = 0; y < size; y++)
        {
            var row = new byte[bytesPerRow];
            for (var x = 0; x < size; x++)
            {
                if (matrix[y / scale][x / scale])
                {
                    row[x / 8] |= (byte)(0x80 >> (x % 8));
                }
            }

            image.AddRange(row);
        }

        return [.. image];
    }
}

using PosNfce.Fiscal.Application.Documents;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Printing;

/// <summary>How the computer reaches the printer.</summary>
public enum PrinterConnection
{
    /// <summary>A printer installed in Windows (USB, or any port its driver handles); ESC/POS bytes go through the spooler as RAW.</summary>
    WindowsQueue,

    /// <summary>A network printer listening for raw data (usually TCP port 9100).</summary>
    Network,
}

/// <summary>Character table used for accented letters. Printers differ here more than anywhere else.</summary>
public enum PrinterCodePage
{
    /// <summary>Accents removed ("Eletrônica" → "Eletronica"): works on every printer.</summary>
    NoAccents,
    Pc850,
    Pc860,
    Windows1252,
}

/// <summary>How the QR Code is printed.</summary>
public enum QrCodeMode
{
    /// <summary>The printer draws it from the text (ESC/POS <c>GS ( k</c>). Sharper; most current printers support it.</summary>
    Native,

    /// <summary>Sent as a bitmap (<c>GS v 0</c>), for printers without the QR command.</summary>
    Image,
}

/// <summary>
/// The receipt printer of this computer. Nothing here is specific to one brand: any printer that speaks ESC/POS
/// (Elgin, Epson, Bematech, Daruma, Tanca, Control iD…) works by choosing the connection, paper width and character table.
/// </summary>
public sealed record ReceiptPrinterSettings(
    PrinterConnection Connection,
    string? PrinterName,
    string? Host,
    int Port,
    int Columns,
    PrinterCodePage CodePage,
    QrCodeMode QrCode,
    bool CutPaper,
    bool PrintAfterAuthorization)
{
    public static IReadOnlyList<int> SupportedColumns { get; } = [48, 42, 32];

    /// <summary>Until the printer is set up: 80 mm paper, no accents, nothing printed automatically.</summary>
    public static ReceiptPrinterSettings Default { get; } =
        new(PrinterConnection.WindowsQueue, null, null, 9100, Danfce.DefaultColumns, PrinterCodePage.NoAccents, QrCodeMode.Native, true, false);

    public bool IsConfigured => Connection switch
    {
        PrinterConnection.WindowsQueue => !string.IsNullOrWhiteSpace(PrinterName),
        PrinterConnection.Network => !string.IsNullOrWhiteSpace(Host),
        _ => false,
    };

    public ReceiptPrinterSettings Validated()
    {
        if (!SupportedColumns.Contains(Columns))
        {
            throw new DomainException("Largura do papel inválida: use 48 ou 42 colunas (80 mm) ou 32 colunas (58 mm).");
        }

        if (Connection == PrinterConnection.WindowsQueue && string.IsNullOrWhiteSpace(PrinterName))
        {
            throw new DomainException("Escolha a impressora instalada no Windows.");
        }

        if (Connection == PrinterConnection.Network)
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                throw new DomainException("Informe o IP da impressora de rede.");
            }

            if (Port is < 1 or > 65535)
            {
                throw new DomainException("Porta inválida: a maioria das impressoras de rede usa 9100.");
            }
        }

        return this with { PrinterName = PrinterName?.Trim(), Host = Host?.Trim() };
    }
}

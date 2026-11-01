using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Application.Printing;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Adapters.Out.Printing;

/// <summary>
/// Sends the ESC/POS bytes to a printer installed in Windows (as a RAW job, so the driver doesn't touch them)
/// or straight to a network printer. The bytes are the same for both.
/// </summary>
public sealed class EscPosReceiptPrinter : IReceiptPrinter
{
    private static readonly TimeSpan NetworkTimeout = TimeSpan.FromSeconds(5);

    public async Task PrintAsync(Danfce receipt, ReceiptPrinterSettings settings, CancellationToken cancellationToken = default)
    {
        var bytes = EscPosReceipt.Encode(receipt, settings);
        switch (settings.Connection)
        {
            case PrinterConnection.WindowsQueue:
                // The spooler call is synchronous; keep it off the screen's thread.
                await Task.Run(() => RawWindowsPrinter.Send(settings.PrinterName!, bytes), cancellationToken);
                break;
            case PrinterConnection.Network:
                await SendToNetworkAsync(settings.Host!, settings.Port, bytes, cancellationToken);
                break;
            default:
                throw new DomainException("Tipo de conexão da impressora desconhecido.");
        }
    }

    private static async Task SendToNetworkAsync(string host, int port, byte[] bytes, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(NetworkTimeout);
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, timeout.Token);
            await using var stream = client.GetStream();
            await stream.WriteAsync(bytes, timeout.Token);
        }
        catch (Exception error) when (error is SocketException or OperationCanceledException or IOException && !cancellationToken.IsCancellationRequested)
        {
            throw new DomainException($"Não foi possível imprimir na impressora de rede {host}:{port}. Confira se ela está ligada e na mesma rede.");
        }
    }
}

/// <summary>Writes raw bytes to a Windows print queue through the spooler API (winspool.drv).</summary>
internal static class RawWindowsPrinter
{
    private const int InvalidPrinterName = 1801;

    public static void Send(string printerName, byte[] bytes)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new DomainException("Impressão pela fila do Windows só funciona no Windows.");
        }

        if (!OpenPrinter(printerName, out var printer, IntPtr.Zero))
        {
            var code = Marshal.GetLastWin32Error();
            throw new DomainException(code == InvalidPrinterName
                ? $"Impressora \"{printerName}\" não encontrada no Windows. Confira o nome na tela \"Impressora\"."
                : $"Não foi possível abrir a impressora \"{printerName}\": {new Win32Exception(code).Message}");
        }

        try
        {
            var document = new DocInfo { DocumentName = "DANFCE", DataType = "RAW" };
            if (!StartDocPrinter(printer, 1, document))
            {
                throw Failure(printerName);
            }

            try
            {
                if (!StartPagePrinter(printer) || !WritePrinter(printer, bytes, bytes.Length, out var written) || written != bytes.Length)
                {
                    throw Failure(printerName);
                }

                EndPagePrinter(printer);
            }
            finally
            {
                EndDocPrinter(printer);
            }
        }
        finally
        {
            ClosePrinter(printer);
        }
    }

    private static DomainException Failure(string printerName) =>
        new($"Falha ao enviar para a impressora \"{printerName}\": {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocInfo
    {
        public string? DocumentName;
        public string? OutputFile;
        public string? DataType;
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(string printerName, out IntPtr printer, IntPtr defaults);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr printer);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool StartDocPrinter(IntPtr printer, int level, [In] DocInfo document);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr printer);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr printer);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr printer);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr printer, byte[] bytes, int count, out int written);
}

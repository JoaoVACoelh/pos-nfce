using System.Net;
using System.Net.Sockets;
using PosNfce.Fiscal.Adapters.Out.Printing;
using PosNfce.Fiscal.Application.Documents;
using PosNfce.Fiscal.Application.Printing;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Adapters;

/// <summary>Real sending paths, without a printer: a local TCP listener plays the network printer.</summary>
public sealed class EscPosReceiptPrinterTests
{
    private static readonly Danfce Receipt = new([new DanfceLine("TESTE", DanfceStyle.CenteredBold)], "", 48, false);

    [Fact]
    public async Task Network_printer_receives_exactly_the_encoded_bytes()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var settings = ReceiptPrinterSettings.Default with { Connection = PrinterConnection.Network, Host = "127.0.0.1", Port = port };

        var received = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var buffer = new MemoryStream();
            await client.GetStream().CopyToAsync(buffer);
            return buffer.ToArray();
        });

        await new EscPosReceiptPrinter().PrintAsync(Receipt, settings);

        Assert.Equal(EscPosReceipt.Encode(Receipt, settings), await received.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task Unreachable_network_printer_gives_a_readable_reason()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop(); // nothing listens on this port now
        var settings = ReceiptPrinterSettings.Default with { Connection = PrinterConnection.Network, Host = "127.0.0.1", Port = port };

        var error = await Assert.ThrowsAsync<DomainException>(() => new EscPosReceiptPrinter().PrintAsync(Receipt, settings));

        Assert.Contains($"127.0.0.1:{port}", error.Message);
    }

    [Fact]
    public async Task Missing_windows_printer_gives_a_readable_reason()
    {
        var settings = ReceiptPrinterSettings.Default with { PrinterName = "Impressora que não existe " + Guid.NewGuid() };

        var error = await Assert.ThrowsAsync<DomainException>(() => new EscPosReceiptPrinter().PrintAsync(Receipt, settings));

        Assert.Contains("não encontrada", error.Message);
    }
}

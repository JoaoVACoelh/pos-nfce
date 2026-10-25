using PosNfce.Fiscal.Application.Printing;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Tests.Application;

public sealed class ReceiptPrinterSettingsTests
{
    [Fact]
    public void Default_is_80_mm_without_accents_and_not_configured()
    {
        var settings = ReceiptPrinterSettings.Default;

        Assert.Equal((48, PrinterCodePage.NoAccents, false, false), (settings.Columns, settings.CodePage, settings.IsConfigured, settings.PrintAfterAuthorization));
    }

    [Theory]
    [InlineData(PrinterConnection.WindowsQueue, null, "192.168.0.50", 9100, 48, "Escolha a impressora")]
    [InlineData(PrinterConnection.Network, "Termica", null, 9100, 48, "IP")]
    [InlineData(PrinterConnection.Network, null, "192.168.0.50", 0, 48, "Porta")]
    [InlineData(PrinterConnection.WindowsQueue, "Termica", null, 9100, 40, "Largura")]
    public void Incomplete_settings_are_refused(PrinterConnection connection, string? name, string? host, int port, int columns, string expected)
    {
        var settings = new ReceiptPrinterSettings(connection, name, host, port, columns, PrinterCodePage.NoAccents, QrCodeMode.Native, true, false);

        Assert.Contains(expected, Assert.Throws<DomainException>(() => settings.Validated()).Message);
    }

    [Fact]
    public void Names_are_trimmed()
    {
        var settings = (ReceiptPrinterSettings.Default with { PrinterName = "  ELGIN i9  " }).Validated();

        Assert.Equal("ELGIN i9", settings.PrinterName);
    }
}

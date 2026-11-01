using System.Collections.ObjectModel;
using System.Printing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Desktop.Shared;
using PosNfce.Fiscal.Application.Printing;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Fiscal.Printer;

public sealed record PrinterOption<T>(T Value, string Label);

/// <summary>
/// The receipt printer of this computer. Any ESC/POS thermal printer works: the cashier picks how it is connected,
/// the paper width and the character table, and checks the result with the test page before saving.
/// </summary>
public sealed partial class PrinterViewModel(UseCaseRunner useCases) : FormViewModel
{
    public IReadOnlyList<PrinterOption<PrinterConnection>> Connections { get; } =
    [
        new(PrinterConnection.WindowsQueue, "Instalada no Windows (USB)"),
        new(PrinterConnection.Network, "Rede (IP)"),
    ];

    public IReadOnlyList<PrinterOption<int>> Widths { get; } =
    [
        new(48, "80 mm - 48 colunas (mais comum)"),
        new(42, "80 mm - 42 colunas"),
        new(32, "58 mm - 32 colunas"),
    ];

    public IReadOnlyList<PrinterOption<PrinterCodePage>> CodePages { get; } =
    [
        new(PrinterCodePage.NoAccents, "Sem acentos (funciona em qualquer impressora)"),
        new(PrinterCodePage.Pc850, "PC850 - Multilíngue"),
        new(PrinterCodePage.Pc860, "PC860 - Português"),
        new(PrinterCodePage.Windows1252, "Windows-1252"),
    ];

    public IReadOnlyList<PrinterOption<QrCodeMode>> QrModes { get; } =
    [
        new(QrCodeMode.Native, "Gerado pela impressora (recomendado)"),
        new(QrCodeMode.Image, "Enviado como imagem (impressoras antigas)"),
    ];

    public ObservableCollection<string> InstalledPrinters { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWindowsQueue), nameof(IsNetwork))]
    public partial PrinterOption<PrinterConnection>? Connection { get; set; }

    [ObservableProperty] public partial string PrinterName { get; set; } = "";
    [ObservableProperty] public partial string Host { get; set; } = "";
    [ObservableProperty] public partial string Port { get; set; } = "9100";
    [ObservableProperty] public partial PrinterOption<int>? Width { get; set; }
    [ObservableProperty] public partial PrinterOption<PrinterCodePage>? CodePage { get; set; }
    [ObservableProperty] public partial PrinterOption<QrCodeMode>? QrMode { get; set; }
    [ObservableProperty] public partial bool CutPaper { get; set; }
    [ObservableProperty] public partial bool PrintAfterAuthorization { get; set; }

    public bool IsWindowsQueue => Connection?.Value == PrinterConnection.WindowsQueue;

    public bool IsNetwork => Connection?.Value == PrinterConnection.Network;

    [RelayCommand]
    public async Task LoadAsync()
    {
        RefreshPrinters();
        var settings = await useCases.RunAsync<GetReceiptPrinterSettings, ReceiptPrinterSettings>(u => u.ExecuteAsync());

        Connection = Connections.First(c => c.Value == settings.Connection);
        PrinterName = settings.PrinterName ?? "";
        if (PrinterName.Length > 0 && !InstalledPrinters.Contains(PrinterName))
        {
            // Saved before but not installed now (unplugged, renamed): keep it visible so the cashier sees what changed.
            InstalledPrinters.Add(PrinterName);
        }
        Host = settings.Host ?? "";
        Port = settings.Port.ToString(Brazilian);
        Width = Widths.FirstOrDefault(w => w.Value == settings.Columns) ?? Widths[0];
        CodePage = CodePages.First(c => c.Value == settings.CodePage);
        QrMode = QrModes.First(q => q.Value == settings.QrCode);
        CutPaper = settings.CutPaper;
        PrintAfterAuthorization = settings.PrintAfterAuthorization;
    }

    /// <summary>Names of the printers installed in Windows, for the list.</summary>
    [RelayCommand]
    private void RefreshPrinters()
    {
        InstalledPrinters.Clear();
        try
        {
            using var server = new LocalPrintServer();
            var queues = server.GetPrintQueues([EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections]);
            foreach (var name in queues.Select(q => q.FullName).Order())
            {
                InstalledPrinters.Add(name);
            }
        }
        catch (PrintSystemException)
        {
            ShowError("Não foi possível listar as impressoras do Windows. Confira se o serviço de impressão (spooler) está ativo.");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var settings = Settings();
            await useCases.RunAsync<SaveReceiptPrinterSettings>(u => u.ExecuteAsync(settings));
            ShowSuccess(settings.PrintAfterAuthorization
                ? "Impressora salva. O cupom sai sozinho depois de cada NFC-e autorizada."
                : "Impressora salva. Imprima pelo botão \"Imprimir DANFCE\" em \"Notas fiscais\".");
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
    }

    [RelayCommand]
    private async Task PrintTestAsync()
    {
        try
        {
            var settings = Settings();
            await useCases.RunAsync<PrintTestPage>(u => u.ExecuteAsync(settings));
            ShowSuccess("Página de teste enviada. Confira a régua, os acentos e o QR Code; se estiver tudo certo, salve.");
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
    }

    private ReceiptPrinterSettings Settings()
    {
        if (!int.TryParse(Port.Trim(), out var port))
        {
            throw new DomainException("Porta inválida: use só números, por exemplo 9100.");
        }

        return new ReceiptPrinterSettings(
            Connection?.Value ?? PrinterConnection.WindowsQueue, PrinterName, Host, port, Width?.Value ?? 48,
            CodePage?.Value ?? PrinterCodePage.NoAccents, QrMode?.Value ?? QrCodeMode.Native, CutPaper, PrintAfterAuthorization);
    }
}

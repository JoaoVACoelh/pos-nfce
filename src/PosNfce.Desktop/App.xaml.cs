using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PosNfce.Catalog.Adapters;
using PosNfce.Checkout.Adapters;
using PosNfce.Fiscal.Adapters;
using PosNfce.Reports.Adapters;

namespace PosNfce.Desktop;

/// <summary>
/// Composition root: the only place that knows every module and wires them together.
/// </summary>
public partial class App : Application
{
    /// <summary>%LOCALAPPDATA%\PosNfce: database and, later, the issued XMLs. Outside the install folder so updates keep the data.</summary>
    private static readonly string DataFolder = Directory.CreateDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PosNfce")).FullName;

    private static readonly string ConnectionString = $"Data Source={Path.Combine(DataFolder, "pos.db")}";

    private readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services
                .AddCatalogModule(ConnectionString)
                .AddCheckoutModule()
                .AddFiscalModule()
                .AddReportsModule();

            services.AddSingleton<MainWindow>();
        })
        .Build();

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.Services.MigrateCatalogDatabaseAsync();
        await _host.StartAsync();
        _host.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}

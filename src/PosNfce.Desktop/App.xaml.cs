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
    private readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services
                .AddCatalogModule()
                .AddCheckoutModule()
                .AddFiscalModule()
                .AddReportsModule();

            services.AddSingleton<MainWindow>();
        })
        .Build();

    protected override async void OnStartup(StartupEventArgs e)
    {
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

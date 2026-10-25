using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PosNfce.Catalog.Adapters;
using PosNfce.Checkout.Adapters;
using PosNfce.Desktop.Catalog.Ibpt;
using PosNfce.Desktop.Checkout;
using PosNfce.Desktop.Catalog.Products;
using PosNfce.Desktop.Catalog.TaxRules;
using PosNfce.Desktop.Fiscal.Issuer;
using PosNfce.Desktop.Shared;
using PosNfce.Fiscal.Adapters;
using PosNfce.Reports.Adapters;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop;

/// <summary>
/// Composition root: the only place that knows every module and wires them together.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// %LOCALAPPDATA%\PosNfce: database and, later, the issued XMLs. Outside the install folder so updates keep the data.
    /// POSNFCE_DATA_FOLDER points elsewhere, so UI tests don't touch the real database.
    /// </summary>
    private static readonly string DataFolder = Directory.CreateDirectory(
        Environment.GetEnvironmentVariable("POSNFCE_DATA_FOLDER")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PosNfce")).FullName;

    private static readonly string ConnectionString = $"Data Source={Path.Combine(DataFolder, "pos.db")}";

    private readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services
                .AddCatalogModule(ConnectionString)
                .AddCheckoutModule(ConnectionString)
                .AddFiscalModule(ConnectionString)
                .AddReportsModule();

            services.AddSingleton<UseCaseRunner>();
            services.AddSingleton<IEventPublisher, ServiceProviderEventPublisher>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddTransient<CheckoutViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<TaxRulesViewModel>();
            services.AddTransient<IssuerViewModel>();
            services.AddTransient<IbptViewModel>();
        })
        .Build();

    protected override async void OnStartup(StartupEventArgs e)
    {
        UseBrazilianFormats();
        await _host.Services.MigrateCatalogDatabaseAsync();
        await _host.Services.MigrateFiscalDatabaseAsync();
        await _host.Services.MigrateCheckoutDatabaseAsync();
        await _host.StartAsync();
        _host.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }

    /// <summary>
    /// WPF formats bindings (StringFormat=C2, dates) with en-US unless told otherwise, whatever the Windows language is.
    /// </summary>
    private static void UseBrazilianFormats()
    {
        var brazilian = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = brazilian;
        CultureInfo.DefaultThreadCurrentUICulture = brazilian;
        Thread.CurrentThread.CurrentCulture = brazilian;
        Thread.CurrentThread.CurrentUICulture = brazilian;
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(brazilian.IetfLanguageTag)));
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}

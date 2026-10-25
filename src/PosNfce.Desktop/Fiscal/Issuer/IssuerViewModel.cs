using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosNfce.Desktop.Shared;
using PosNfce.Fiscal.Application.Issuers;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Desktop.Fiscal.Issuer;

/// <summary>
/// The store's registration data, CSC and A1 certificate. Secrets are write-only here:
/// the screen shows whether they exist, never their content.
/// </summary>
public sealed partial class IssuerViewModel(UseCaseRunner useCases) : FormViewModel
{
    public IReadOnlyList<string> States { get; } = BrazilianState.All.Select(s => s.Code).ToList();

    [ObservableProperty] public partial string Cnpj { get; set; } = "";
    [ObservableProperty] public partial string StateRegistration { get; set; } = "";
    [ObservableProperty] public partial string LegalName { get; set; } = "";
    [ObservableProperty] public partial string TradeName { get; set; } = "";
    [ObservableProperty] public partial string Phone { get; set; } = "";
    [ObservableProperty] public partial string Street { get; set; } = "";
    [ObservableProperty] public partial string Number { get; set; } = "";
    [ObservableProperty] public partial string Complement { get; set; } = "";
    [ObservableProperty] public partial string District { get; set; } = "";
    [ObservableProperty] public partial string CityName { get; set; } = "";
    [ObservableProperty] public partial string CityCode { get; set; } = "";
    [ObservableProperty] public partial string State { get; set; } = "MG";
    [ObservableProperty] public partial string PostalCode { get; set; } = "";
    [ObservableProperty] public partial string Series { get; set; } = "1";
    [ObservableProperty] public partial string NextNumber { get; set; } = "1";
    [ObservableProperty] public partial string CscTokenId { get; set; } = "";
    [ObservableProperty] public partial string Csc { get; set; } = "";

    /// <summary>Send each sale's NFC-e to SEFAZ when the sale closes. Only with the real certificate and CSC.</summary>
    [ObservableProperty] public partial bool AutoIssue { get; set; }

    [ObservableProperty] public partial string CscStatus { get; set; } = "";
    [ObservableProperty] public partial string CertificateStatus { get; set; } = "";
    [ObservableProperty] public partial bool CertificateNeedsAttention { get; set; }

    /// <summary>File chosen in the dialog; read only when saving.</summary>
    [ObservableProperty] public partial string CertificatePath { get; set; } = "";

    /// <summary>Set by the view from the PasswordBox, which can't be bound.</summary>
    public string CertificatePassword { private get; set; } = "";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var issuer = await useCases.RunAsync<GetIssuer, IssuerDetails?>(u => u.ExecuteAsync());
        if (issuer is null)
        {
            CscStatus = "CSC ainda não cadastrado.";
            ShowCertificate(null);
            return;
        }

        Cnpj = issuer.Cnpj;
        StateRegistration = issuer.StateRegistration;
        LegalName = issuer.LegalName;
        TradeName = issuer.TradeName ?? "";
        Phone = issuer.Phone ?? "";
        Street = issuer.Street;
        Number = issuer.Number;
        Complement = issuer.Complement ?? "";
        District = issuer.District;
        CityName = issuer.CityName;
        CityCode = issuer.CityCode;
        State = issuer.State;
        PostalCode = issuer.PostalCode;
        Series = issuer.Series.ToString(Brazilian);
        NextNumber = issuer.NextNumber.ToString(Brazilian);
        CscTokenId = issuer.CscTokenId ?? "";
        AutoIssue = issuer.AutoIssue;
        Csc = "";
        CscStatus = issuer.HasCsc
            ? "CSC cadastrado. Deixe o campo vazio para manter o atual."
            : "CSC ainda não cadastrado.";
        ShowCertificate(issuer.Certificate);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            byte[]? pfx = null;
            if (CertificatePath.Length > 0)
            {
                pfx = await File.ReadAllBytesAsync(CertificatePath);
            }

            var command = new SaveIssuerCommand(
                Cnpj, StateRegistration, LegalName, TradeName,
                Street, Number, Complement, District, CityName, CityCode, State, PostalCode, Phone,
                int.TryParse(Series.Trim(), out var series) ? series : throw new DomainException("Série deve ser um número inteiro."),
                ParseInteger(NextNumber, "Próximo número"),
                CscTokenId, Csc, pfx, CertificatePassword, AutoIssue);

            await useCases.RunAsync<SaveIssuer>(u => u.ExecuteAsync(command));

            CertificatePath = "";
            await LoadAsync();
            ShowSuccess("Dados do emitente salvos.");
        }
        catch (DomainException error)
        {
            ShowError(error.Message);
        }
        catch (IOException error)
        {
            ShowError($"Não foi possível ler o arquivo do certificado: {error.Message}");
        }
    }

    private void ShowCertificate(CertificateSummary? certificate)
    {
        if (certificate is null)
        {
            CertificateStatus = "Nenhum certificado enviado.";
            CertificateNeedsAttention = true;
            return;
        }

        var daysLeft = (certificate.NotAfter.Date - DateTime.Today).Days;
        CertificateStatus = $"Certificado do CNPJ {certificate.Cnpj}, válido até {certificate.NotAfter:dd/MM/yyyy} ({daysLeft} dias).";
        // Stage 6 adds the 30-day alert; the screen already highlights it.
        CertificateNeedsAttention = daysLeft <= 30;
    }

    private static long ParseInteger(string text, string field) =>
        long.TryParse(text.Trim(), out var value) ? value : throw new DomainException($"{field} deve ser um número inteiro.");
}

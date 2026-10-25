using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace PosNfce.Desktop.Fiscal.Issuer;

public partial class IssuerView : UserControl
{
    public IssuerView()
    {
        InitializeComponent();
    }

    private IssuerViewModel ViewModel => (IssuerViewModel)DataContext;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();
        CnpjBox.Focus();
    }

    // File dialogs and PasswordBox are view concerns: the ViewModel only receives the chosen path and the typed password.
    private void OnChooseCertificate(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Certificado digital A1",
            Filter = "Certificado A1 (*.pfx;*.p12)|*.pfx;*.p12",
        };

        if (dialog.ShowDialog() == true)
        {
            ViewModel.CertificatePath = dialog.FileName;
        }
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e) =>
        ViewModel.CertificatePassword = CertificatePasswordBox.Password;
}

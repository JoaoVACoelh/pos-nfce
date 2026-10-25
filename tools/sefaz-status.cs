// Stage 0 "Done when": asks SEFAZ-MG (homologation) whether the NFC-e service is up, using the store's REAL A1 certificate.
// Expected answer: cStat 107 "Servico em Operacao". The development certificate is refused by SEFAZ.
//
// Usage, from the repository root (the password is asked on screen, never passed on the command line):
//   dotnet run tools/sefaz-status.cs -- "C:\path\to\certificate.pfx"

#:package Unimake.DFe
#:property PublishAot=false
#:property NoWarn=$(NoWarn);NU1701

using System.Security.Cryptography.X509Certificates;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Xml.NFe;
using NFCe = Unimake.Business.DFe.Servicos.NFCe;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.WriteLine("Usage: dotnet run tools/sefaz-status.cs -- \"C:\\path\\to\\certificate.pfx\"");
    return 1;
}

Console.Write("Certificate password: ");
var password = ReadPassword();

using var certificate = X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(args[0]), password, X509KeyStorageFlags.EphemeralKeySet);
Console.WriteLine($"Certificate: {certificate.Subject} (valid until {certificate.NotAfter:dd/MM/yyyy})");

var configuration = new Configuracao
{
    TipoDFe = TipoDFe.NFCe,
    TipoEmissao = TipoEmissao.Normal,
    TipoAmbiente = TipoAmbiente.Homologacao,
    CodigoUF = (int)UFBrasil.MG,
    CertificadoDigital = certificate,
};

var service = new NFCe.StatusServico(new ConsStatServ
{
    Versao = "4.00",
    CUF = UFBrasil.MG,
    TpAmb = TipoAmbiente.Homologacao,
}, configuration);
service.Executar();

var result = service.Result;
Console.WriteLine($"cStat {result.CStat}: {result.XMotivo}");
return result.CStat == 107 ? 0 : 2;

static string ReadPassword()
{
    var password = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return password.ToString();
        }

        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password.Length--;
        }
        else if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
        }
    }
}

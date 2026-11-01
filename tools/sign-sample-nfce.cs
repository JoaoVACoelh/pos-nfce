// Builds a sample NFC-e (homologation, fictitious data), signs it with the development certificate and validates it
// against the official schemas embedded in Unimake.DFe. Nothing is sent to SEFAZ.
//
// Usage, from the repository root:
//   dotnet run tools/create-dev-certificate.cs     (once)
//   dotnet run tools/sign-sample-nfce.cs
// Output: data/sample-nfce-signed.xml (ignored by Git).

#:package Unimake.DFe
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CS0618;NU1701

using System.Security.Cryptography.X509Certificates;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Xml.NFe;
using NFCe = Unimake.Business.DFe.Servicos.NFCe;

var root = Directory.GetCurrentDirectory();
var pfx = File.ReadAllBytes(Path.Combine(root, "data", "dev-certificate.pfx"));
using var certificate = X509CertificateLoader.LoadPkcs12(pfx, "dev-only", X509KeyStorageFlags.EphemeralKeySet);

var configuration = new Configuracao
{
    TipoDFe = TipoDFe.NFCe,
    TipoEmissao = TipoEmissao.Normal,
    TipoAmbiente = TipoAmbiente.Homologacao,
    CodigoUF = (int)UFBrasil.MG,
    CertificadoDigital = certificate,
    CSC = "TESTE0123456789ABCDEF",
    CSCIDToken = 1,
};

var issuedAt = new DateTimeOffset(2026, 10, 25, 10, 0, 0, TimeSpan.FromHours(-3));
var batch = new EnviNFe
{
    Versao = "4.00",
    IdLote = "000000000000001",
    IndSinc = SimNao.Sim,
    NFe =
    [
        new NFe
        {
            InfNFeField = new InfNFe
            {
                Versao = "4.00",
                Ide = new Ide
                {
                    CUF = UFBrasil.MG, NatOp = "VENDA", Mod = ModeloDFe.NFCe, Serie = 1, NNF = 1, DhEmi = issuedAt, CNF = "12345678",
                    TpNF = TipoOperacao.Saida, IdDest = DestinoOperacao.OperacaoInterna, CMunFG = 3106200,
                    TpImp = FormatoImpressaoDANFE.NFCe, TpEmis = TipoEmissao.Normal, TpAmb = TipoAmbiente.Homologacao,
                    FinNFe = FinalidadeNFe.Normal, IndFinal = SimNao.Sim, IndPres = IndicadorPresenca.OperacaoPresencial,
                    ProcEmi = ProcessoEmissao.AplicativoContribuinte, VerProc = "PosNfce 0.1",
                },
                Emit = new Emit
                {
                    CNPJ = "11222333000181", XNome = "MERCADINHO EXEMPLO LTDA (TESTE)", IE = "0000000000000", CRT = CRT.SimplesNacional,
                    EnderEmit = new EnderEmit
                    {
                        XLgr = "Rua de Teste", Nro = "123", XBairro = "Centro", CMun = 3106200, XMun = "Belo Horizonte",
                        UF = UFBrasil.MG, CEP = "30110000", CPais = 1058, XPais = "BRASIL",
                    },
                },
                Det =
                [
                    new Det
                    {
                        NItem = 1,
                        Prod = new Prod
                        {
                            // In homologation the first item's description must be this fixed text.
                            CProd = "BEB-001", CEAN = "SEM GTIN", XProd = "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL",
                            NCM = "22021000", CFOP = "5102", UCom = "UN", QCom = 2, VUnCom = 5.49m, VProd = 10.98,
                            CEANTrib = "SEM GTIN", UTrib = "UN", QTrib = 2, VUnTrib = 5.49m, VDesc = 0.98, IndTot = SimNao.Sim,
                        },
                        Imposto = new Imposto
                        {
                            // Format example only: the real tax groups come from the TaxCalculator (pending confirmation of the rules).
                            ICMS = new ICMS { ICMSSN102 = new ICMSSN102 { Orig = OrigemMercadoria.Nacional, CSOSN = "102" } },
                            PIS = new PIS { PISOutr = new PISOutr { CST = "49", VBC = 0, PPIS = 0, VPIS = 0 } },
                            COFINS = new COFINS { COFINSOutr = new COFINSOutr { CST = "49", VBC = 0, PCOFINS = 0, VCOFINS = 0 } },
                        },
                    },
                ],
                Total = new Total { ICMSTot = new ICMSTot { VProd = 10.98, VDesc = 0.98, VNF = 10.00 } },
                Transp = new Transp { ModFrete = ModalidadeFrete.SemOcorrenciaTransporte },
                Pag = new Pag { DetPag = [new DetPag { TPag = MeioPagamento.Dinheiro, VPag = 20.00 }], VTroco = 10.00 },
            },
        },
    ],
};

// The service builds the XML, signs it and validates it against the schema in its constructor; Executar() would send it.
var service = new NFCe.Autorizacao(batch, configuration);
var output = Path.Combine(root, "data", "sample-nfce-signed.xml");
File.WriteAllText(output, service.ConteudoXMLAssinado.OuterXml);

Console.WriteLine("Signed and valid against the NF-e schema. Nothing was sent.");
Console.WriteLine($"Access key: {batch.NFe[0].InfNFeField.Chave}");
Console.WriteLine($"File:       {output}");

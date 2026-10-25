using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.Fiscal.Domain.Taxes;
using PosNfce.SharedKernel;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Xml.NFe;
using NFCe = Unimake.Business.DFe.Servicos.NFCe;
using UnimakeNFe = Unimake.Business.DFe.Xml.NFe.NFe;

namespace PosNfce.Fiscal.Adapters.Out.Sefaz;

/// <summary>
/// Translates an <see cref="NfceDraft"/> into Unimake.DFe's XML classes, signs and validates it.
/// All decisions (taxes, totals, texts) are already in the draft; this class only maps fields.
/// Unimake uses <c>double</c> for most values: they are converted here, from decimals already rounded to cents.
/// </summary>
public sealed class UnimakeNfceIssuer : IFiscalDocumentIssuer
{
    private const int Brazil = 1058;

    public SignedNfce Sign(NfceDraft draft, IssuerCertificate certificate, Csc csc)
    {
        using var x509 = X509CertificateLoader.LoadPkcs12(certificate.Pfx, certificate.Password, X509KeyStorageFlags.EphemeralKeySet);
        var environment = draft.Homologation ? TipoAmbiente.Homologacao : TipoAmbiente.Producao;
        var state = Enum.Parse<UFBrasil>(draft.Issuer.Address.State.Code);

        var configuration = new Configuracao
        {
            TipoDFe = TipoDFe.NFCe,
            TipoEmissao = TipoEmissao.Normal,
            TipoAmbiente = environment,
            CodigoUF = (int)state,
            CertificadoDigital = x509,
            CSC = csc.Token,
            CSCIDToken = int.Parse(csc.TokenId),
        };

        var nfe = new UnimakeNFe { InfNFeField = InfNFe(draft, environment, state) };
        var batch = new EnviNFe { Versao = "4.00", IdLote = draft.Number.ToString("D15"), IndSinc = SimNao.Sim, NFe = [nfe] };

        try
        {
            // The constructor builds, signs and validates against the embedded schema; Executar() would send it.
            var service = new NFCe.Autorizacao(batch, configuration);
            return new SignedNfce(nfe.InfNFeField.Chave, service.ConteudoXMLAssinado.OuterXml, nfe.InfNFeSupl?.QrCode ?? "");
        }
        catch (Exception error) when (error is not DomainException and not CryptographicException)
        {
            throw new DomainException($"O XML da nota não passou na validação: {error.Message}");
        }
    }

    private static InfNFe InfNFe(NfceDraft draft, TipoAmbiente environment, UFBrasil state)
    {
        var issuer = draft.Issuer;
        var address = issuer.Address;

        return new InfNFe
        {
            Versao = "4.00",
            Ide = new Ide
            {
                CUF = state,
                NatOp = "VENDA",
                Mod = ModeloDFe.NFCe,
                Serie = draft.Series,
                NNF = checked((int)draft.Number),
                CNF = draft.NumericCode,
                DhEmi = draft.IssuedAt,
                TpNF = TipoOperacao.Saida,
                IdDest = DestinoOperacao.OperacaoInterna,
                CMunFG = int.Parse(address.CityCode),
                TpImp = FormatoImpressaoDANFE.NFCe,
                TpEmis = TipoEmissao.Normal,
                TpAmb = environment,
                FinNFe = FinalidadeNFe.Normal,
                IndFinal = SimNao.Sim,
                IndPres = IndicadorPresenca.OperacaoPresencial,
                ProcEmi = ProcessoEmissao.AplicativoContribuinte,
                VerProc = "PosNfce 0.1",
            },
            Emit = new Emit
            {
                CNPJ = issuer.Cnpj.Value,
                XNome = issuer.LegalName,
                XFant = issuer.TradeName,
                IE = issuer.StateRegistration,
                CRT = CRT.SimplesNacional,
                EnderEmit = new EnderEmit
                {
                    XLgr = address.Street,
                    Nro = address.Number,
                    XCpl = address.Complement,
                    XBairro = address.District,
                    CMun = int.Parse(address.CityCode),
                    XMun = address.CityName,
                    UF = state,
                    CEP = address.PostalCode,
                    CPais = Brazil,
                    XPais = "BRASIL",
                    Fone = issuer.Phone,
                },
            },
            Det = draft.Items.Select(Det).ToList(),
            Total = Total(draft.Totals),
            Transp = new Transp { ModFrete = ModalidadeFrete.SemOcorrenciaTransporte },
            Pag = new Pag
            {
                DetPag = draft.Payments.Select(DetPag).ToList(),
                VTroco = D(draft.Change),
            },
            InfAdic = draft.AdditionalInfo.Length == 0 ? null : new InfAdic { InfCpl = draft.AdditionalInfo },
        };
    }

    private static Det Det(NfceItem line)
    {
        var item = line.Item;
        var taxes = line.Taxes;

        return new Det
        {
            NItem = item.Line,
            Prod = new Prod
            {
                CProd = item.Code,
                CEAN = item.Gtin,
                XProd = line.Description,
                NCM = item.Ncm,
                CEST = item.Cest,
                CFOP = item.Cfop,
                UCom = item.Unit,
                QCom = item.Quantity,
                VUnCom = item.UnitPrice.Amount,
                VProd = D(item.GrossTotal),
                CEANTrib = item.Gtin,
                UTrib = item.Unit,
                QTrib = item.Quantity,
                VUnTrib = item.UnitPrice.Amount,
                VDesc = D(item.Discount),
                IndTot = SimNao.Sim,
            },
            Imposto = new Imposto
            {
                VTotTrib = D(taxes.Approximate.Total),
                ICMS = Icms(taxes.Icms),
                PIS = Pis(taxes.Pis),
                COFINS = Cofins(taxes.Cofins),
                IBSCBS = taxes.IbsCbs is null ? null : IbsCbs(taxes.IbsCbs),
            },
        };
    }

    private static ICMS Icms(SimplesIcms icms) => icms.Group switch
    {
        SimplesIcmsGroup.Sn102 => new ICMS { ICMSSN102 = new ICMSSN102 { Orig = (OrigemMercadoria)icms.Origin, CSOSN = icms.Csosn } },
        SimplesIcmsGroup.Sn500 => new ICMS { ICMSSN500 = new ICMSSN500 { Orig = (OrigemMercadoria)icms.Origin, CSOSN = icms.Csosn } },
        _ => throw new InvalidOperationException($"Unknown ICMS group {icms.Group}."),
    };

    private static PIS Pis(PisCofins pis) => pis.Group switch
    {
        PisCofinsGroup.Rate => new PIS { PISAliq = new PISAliq { CST = pis.Cst, VBC = D(pis.Base), PPIS = (double)pis.Rate, VPIS = D(pis.Value) } },
        PisCofinsGroup.NotTaxed => new PIS { PISNT = new PISNT { CST = pis.Cst } },
        _ => new PIS { PISOutr = new PISOutr { CST = pis.Cst, VBC = D(pis.Base), PPIS = (double)pis.Rate, VPIS = D(pis.Value) } },
    };

    private static COFINS Cofins(PisCofins cofins) => cofins.Group switch
    {
        PisCofinsGroup.Rate => new COFINS { COFINSAliq = new COFINSAliq { CST = cofins.Cst, VBC = D(cofins.Base), PCOFINS = (double)cofins.Rate, VCOFINS = D(cofins.Value) } },
        PisCofinsGroup.NotTaxed => new COFINS { COFINSNT = new COFINSNT { CST = cofins.Cst } },
        _ => new COFINS { COFINSOutr = new COFINSOutr { CST = cofins.Cst, VBC = D(cofins.Base), PCOFINS = (double)cofins.Rate, VCOFINS = D(cofins.Value) } },
    };

    private static IBSCBS IbsCbs(IbsCbsTaxes t)
    {
        var group = new IBSCBS { CST = t.Cst, CClassTrib = t.ClassTrib };
        if (!t.Taxed)
        {
            return group;
        }

        // Rates in the XML are the full ones; with a reduction, gRed carries the reduction and the effective rate.
        GRed? Reduction(decimal effective) =>
            t.Reduction == 0m ? null : new GRed { PRedAliq = (double)t.Reduction, PAliqEfet = (double)effective };

        group.GIBSCBS = new GIBSCBS
        {
            VBC = D(t.Base),
            GIBSUF = new GIBSUF { PIBSUF = (double)FullRate(t.IbsStateRate, t.Reduction), GRed = Reduction(t.IbsStateRate), VIBSUF = D(t.IbsState) },
            GIBSMun = new GIBSMun { PIBSMun = (double)FullRate(t.IbsMunicipalRate, t.Reduction), GRed = Reduction(t.IbsMunicipalRate), VIBSMun = D(t.IbsMunicipal) },
            VIBS = D(t.Ibs),
            GCBS = new GCBS { PCBS = (double)FullRate(t.CbsRate, t.Reduction), GRed = Reduction(t.CbsRate), VCBS = D(t.Cbs) },
        };
        return group;
    }

    /// <summary>Back from the effective rate to the full one: effective = full × (1 − reduction / 100).</summary>
    private static decimal FullRate(decimal effective, decimal reduction) =>
        reduction == 0m ? effective : decimal.Round(effective / (1m - reduction / 100m), 4);

    private static Total Total(NfceTotals totals)
    {
        var total = new Total
        {
            ICMSTot = new ICMSTot
            {
                VProd = D(totals.Products),
                VDesc = D(totals.Discount),
                VPIS = D(totals.Pis),
                VCOFINS = D(totals.Cofins),
                VNF = D(totals.Total),
                VTotTrib = D(totals.Approximate.Total),
            },
        };

        if (totals.HasIbsCbs)
        {
            total.IBSCBSTot = new IBSCBSTot
            {
                VBCIBSCBS = D(totals.IbsCbsBase!.Value),
                GIBS = new GIBSTot
                {
                    GIBSUF = new GIBSUFTot { VIBSUF = D(totals.IbsState!.Value) },
                    GIBSMun = new GIBSMunTot { VIBSMun = D(totals.IbsMunicipal!.Value) },
                    VIBS = D(totals.IbsState.Value + totals.IbsMunicipal.Value),
                },
                GCBS = new GCBSTot { VCBS = D(totals.Cbs!.Value) },
            };
        }

        return total;
    }

    private static DetPag DetPag(NfcePayment payment) => new()
    {
        TPag = (MeioPagamento)(int)payment.Kind,
        VPag = D(payment.Amount),
        Card = payment.HasCardGroup
            ? new Card { TpIntegra = payment.Integrated ? TipoIntegracaoPagamento.PagamentoIntegrado : TipoIntegracaoPagamento.PagamentoNaoIntegrado }
            : null,
    };

    private static double D(Money money) => (double)money.Amount;
}

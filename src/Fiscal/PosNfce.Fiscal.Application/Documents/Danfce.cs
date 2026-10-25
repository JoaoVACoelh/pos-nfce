using System.Globalization;
using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Application.Documents;

public enum DanfceStyle
{
    Normal,
    Bold,
    Centered,
    CenteredBold,
    Small,

    /// <summary>A full-width separator line.</summary>
    Rule,
}

public sealed record DanfceLine(string Text, DanfceStyle Style = DanfceStyle.Normal);

/// <summary>
/// The consumer receipt of an NFC-e (DANFCE) as text lines for an 80 mm printer, plus the QR Code content.
/// The same lines feed the screen preview, a Windows printer and, later, the ESC/POS printer.
/// </summary>
public sealed record Danfce(IReadOnlyList<DanfceLine> Lines, string QrCode, int Width, bool Authorized)
{
    /// <summary>Characters per line on an 80 mm thermal printer with the standard font.</summary>
    public const int Columns = 48;
}

/// <summary>
/// Lays out the DANFCE following the order of the "Manual de Especificações Técnicas do DANFE NFC-e e QR Code":
/// issuer, title, items, totals, payments, consultation key, consumer, identification and protocol, QR Code,
/// approximate taxes and, in homologation, the "no fiscal value" message.
/// </summary>
public static class DanfceLayout
{
    private static readonly CultureInfo Brazilian = CultureInfo.GetCultureInfo("pt-BR");

    public static Danfce Build(FiscalDocument document, IssuerProfile issuer, bool homologation)
    {
        if (document.AccessKey is null || document.Number is null)
        {
            throw new DomainException("A nota ainda não foi assinada: gere o XML ou emita antes de ver o DANFCE.");
        }

        var lines = new List<DanfceLine>();
        void Add(string text, DanfceStyle style = DanfceStyle.Normal) => lines.Add(new DanfceLine(text, style));
        void Rule() => lines.Add(new DanfceLine(new string('-', Danfce.Columns), DanfceStyle.Rule));

        // Issuer
        Add(issuer.LegalName, DanfceStyle.CenteredBold);
        Add($"CNPJ {issuer.Cnpj.Formatted}  IE {issuer.StateRegistration}", DanfceStyle.Centered);
        var address = issuer.Address;
        Add($"{address.Street}, {address.Number}{(address.Complement is null ? "" : " " + address.Complement)}", DanfceStyle.Centered);
        Add($"{address.District} - {address.CityName} - {address.State.Code}", DanfceStyle.Centered);
        Rule();
        Add("Documento Auxiliar da Nota Fiscal", DanfceStyle.Centered);
        Add("de Consumidor Eletrônica", DanfceStyle.Centered);
        Rule();

        // Items
        Add(Columns("#  CÓDIGO / DESCRIÇÃO", "QTD UN  VL UNIT  VL TOTAL"), DanfceStyle.Small);
        foreach (var item in document.Items)
        {
            var description = homologation && item.Line == 1 ? NfceDraft.HomologationDescription : item.Description;
            foreach (var part in Wrap($"{item.Line:000} {item.Code} {description}", Danfce.Columns))
            {
                Add(part);
            }

            Add(Columns("", $"{Quantity(item.Quantity)} {item.Unit} x {Value(item.UnitPrice)} = {Value(item.GrossTotal)}"));
        }

        Rule();

        // Totals and payments
        Add(Columns("Qtd. total de itens", document.Items.Count.ToString(Brazilian)));
        Add(Columns("Valor total R$", Value(document.GrossTotal)));
        if (document.Discount > Money.Zero)
        {
            Add(Columns("Desconto R$", Value(document.Discount)));
        }

        Add(Columns("Valor a pagar R$", Value(document.Total)), DanfceStyle.Bold);
        Add(Columns("FORMA DE PAGAMENTO", "VALOR PAGO R$"), DanfceStyle.Small);
        foreach (var payment in document.Payments)
        {
            Add(Columns(PaymentName(payment.Method), Value(payment.Amount)));
        }

        if (document.Change > Money.Zero)
        {
            Add(Columns("Troco R$", Value(document.Change)));
        }

        Rule();

        // Consultation
        Add("Consulte pela Chave de Acesso em", DanfceStyle.Centered);
        Add(document.ConsultUrl ?? "", DanfceStyle.Centered);
        Add(GroupedKey(document.AccessKey), DanfceStyle.Centered);
        Rule();

        // Consumer
        Add(document.CustomerCpf is { } cpf ? $"CONSUMIDOR - CPF {FormatCpf(cpf)}" : "CONSUMIDOR NÃO IDENTIFICADO", DanfceStyle.Centered);
        Rule();

        // Identification and protocol
        Add($"NFC-e nº {document.Number:000000000}  Série {document.Series:000}", DanfceStyle.CenteredBold);
        Add(document.SoldAt.ToString("dd/MM/yyyy HH:mm:ss", Brazilian), DanfceStyle.Centered);
        if (document.Status == FiscalDocumentStatus.Authorized)
        {
            Add($"Protocolo de autorização: {document.Protocol}", DanfceStyle.Centered);
            Add($"Data de autorização: {document.AuthorizedAt:dd/MM/yyyy HH:mm:ss}", DanfceStyle.Centered);
        }
        else
        {
            Add("PRÉVIA - NOTA AINDA NÃO AUTORIZADA PELA SEFAZ", DanfceStyle.CenteredBold);
        }

        // The QR Code goes here (the printer or the preview draws it between these lines).
        Add("[QRCODE]", DanfceStyle.Centered);

        // Approximate taxes (Lei 12.741/2012) and homologation notice
        if (!string.IsNullOrEmpty(document.AdditionalInfo))
        {
            Rule();
            foreach (var part in Wrap(document.AdditionalInfo, Danfce.Columns))
            {
                Add(part, DanfceStyle.Small);
            }
        }

        if (homologation)
        {
            Rule();
            Add("EMITIDA EM AMBIENTE DE HOMOLOGAÇÃO", DanfceStyle.CenteredBold);
            Add("SEM VALOR FISCAL", DanfceStyle.CenteredBold);
        }

        return new Danfce(lines, document.QrCode ?? "", Danfce.Columns, document.Status == FiscalDocumentStatus.Authorized);
    }

    /// <summary>Left text and right text on one line, padded with spaces; the left side is cut if both don't fit.</summary>
    internal static string Columns(string left, string right)
    {
        var space = Danfce.Columns - right.Length - 1;
        if (space <= 0)
        {
            return right.Length > Danfce.Columns ? right[..Danfce.Columns] : right;
        }

        var cut = left.Length > space ? left[..space] : left;
        return cut + new string(' ', Danfce.Columns - cut.Length - right.Length) + right;
    }

    internal static IEnumerable<string> Wrap(string text, int width)
    {
        var line = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return line;
                line = "";
            }

            line = line.Length == 0 ? word : $"{line} {word}";
            while (line.Length > width)
            {
                yield return line[..width];
                line = line[width..];
            }
        }

        if (line.Length > 0)
        {
            yield return line;
        }
    }

    private static string Value(Money money) => money.Amount.ToString("#,##0.00", Brazilian);

    private static string Quantity(decimal quantity) => quantity.ToString("0.####", Brazilian);

    private static string GroupedKey(string key) => string.Join(' ', Enumerable.Range(0, 11).Select(i => key.Substring(i * 4, 4)));

    private static string FormatCpf(string cpf) =>
        cpf.Length == 11 ? $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}" : cpf;

    private static string PaymentName(string method) => method switch
    {
        "Cash" => "Dinheiro",
        "CreditCard" => "Cartão de Crédito",
        "DebitCard" => "Cartão de Débito",
        "Pix" => "PIX",
        _ => method,
    };
}

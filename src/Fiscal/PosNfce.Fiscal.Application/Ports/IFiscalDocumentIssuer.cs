using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;

namespace PosNfce.Fiscal.Application.Ports;

/// <summary>
/// Writes the NFC-e XML with a fiscal library, signs it with the issuer's certificate, validates it against the
/// official schema and sends it to the state's SEFAZ.
/// </summary>
public interface IFiscalDocumentIssuer
{
    /// <summary>Throws <see cref="SharedKernel.DomainException"/> when the XML doesn't pass the schema validation.</summary>
    SignedNfce Sign(NfceDraft draft, IssuerCertificate certificate, Csc csc);

    /// <summary>
    /// Sends the signed XML synchronously. Never throws for SEFAZ or network problems: they come back as
    /// <see cref="SefazOutcome.NotSent"/> or <see cref="SefazOutcome.Rejected"/>.
    /// </summary>
    Task<SefazAnswer> SendAsync(SignedNfce signed, IssuerCertificate certificate, IssuerProfile issuer, bool homologation,
        CancellationToken cancellationToken = default);
}

public sealed record SignedNfce(string AccessKey, string Xml, string QrCode);

public enum SefazOutcome
{
    Authorized,
    Rejected,

    /// <summary>SEFAZ couldn't be reached (no internet, timeout, service down).</summary>
    NotSent,
}

/// <summary>SEFAZ answer. <paramref name="AuthorizedXml"/>: the XML with the protocol (nfeProc), to keep for 5 years.</summary>
public sealed record SefazAnswer(
    SefazOutcome Outcome, int? StatusCode, string Message, string? Protocol, DateTimeOffset? ReceivedAt, string? AuthorizedXml);

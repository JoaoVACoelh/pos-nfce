using PosNfce.Fiscal.Domain.Documents;
using PosNfce.Fiscal.Domain.Issuers;

namespace PosNfce.Fiscal.Application.Ports;

/// <summary>
/// Writes the NFC-e XML with a fiscal library, signs it with the issuer's certificate and validates it against the
/// official schema. Sending to SEFAZ is added in Stage 3 once the real certificate and CSC exist.
/// </summary>
public interface IFiscalDocumentIssuer
{
    /// <summary>Throws <see cref="SharedKernel.DomainException"/> when the XML doesn't pass the schema validation.</summary>
    SignedNfce Sign(NfceDraft draft, IssuerCertificate certificate, Csc csc);
}

public sealed record SignedNfce(string AccessKey, string Xml, string QrCode);

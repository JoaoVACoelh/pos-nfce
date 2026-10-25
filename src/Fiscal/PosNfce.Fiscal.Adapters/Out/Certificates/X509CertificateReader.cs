using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PosNfce.Fiscal.Application.Ports;
using PosNfce.Fiscal.Domain.Issuers;
using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Adapters.Out.Certificates;

/// <summary>
/// Reads an ICP-Brasil e-CNPJ A1 (.pfx). The CNPJ comes from the SubjectAltName otherName 2.16.76.1.3.3;
/// if it's missing, from the subject CN, which ICP-Brasil formats as "COMPANY NAME:CNPJ".
/// </summary>
public sealed class X509CertificateReader : ICertificateReader
{
    private const string SubjectAltNameOid = "2.5.29.17";
    private const string CnpjOid = "2.16.76.1.3.3";

    public CertificateInfo Read(byte[] pfx, string password)
    {
        X509Certificate2 certificate;
        try
        {
            certificate = X509CertificateLoader.LoadPkcs12(pfx, password, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (CryptographicException)
        {
            throw new DomainException("Não foi possível abrir o certificado: senha incorreta ou arquivo inválido.");
        }

        using (certificate)
        {
            if (!certificate.HasPrivateKey)
            {
                throw new DomainException("O arquivo não tem a chave privada. Use o certificado A1 completo (.pfx).");
            }

            var cnpj = CnpjFromSubjectAltName(certificate) ?? CnpjFromCommonName(certificate)
                ?? throw new DomainException("O certificado não é um e-CNPJ: não foi encontrado o CNPJ da empresa.");

            return new CertificateInfo(Cnpj.Parse(cnpj), certificate.Subject, new DateTimeOffset(certificate.NotAfter));
        }
    }

    private static string? CnpjFromSubjectAltName(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions[SubjectAltNameOid];
        if (extension is null)
        {
            return null;
        }

        try
        {
            var names = new AsnReader(extension.RawData, AsnEncodingRules.DER).ReadSequence();
            while (names.HasData)
            {
                var tag = names.PeekTag();
                if (tag.TagClass != TagClass.ContextSpecific || tag.TagValue != 0)
                {
                    names.ReadEncodedValue();
                    continue;
                }

                // otherName ::= SEQUENCE { type-id OID, value [0] EXPLICIT ANY }
                var otherName = names.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true));
                var oid = otherName.ReadObjectIdentifier();
                var value = otherName.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true));
                if (oid == CnpjOid)
                {
                    // Issuers encode it as OCTET STRING or as a character string; either way the CNPJ is its digits.
                    var raw = value.ReadEncodedValue().Span;
                    var digits = new string(System.Text.Encoding.ASCII.GetString(raw).Where(char.IsAsciiDigit).ToArray());
                    return digits.Length >= 14 ? digits[^14..] : null;
                }
            }
        }
        catch (AsnContentException)
        {
            return null;
        }

        return null;
    }

    private static string? CnpjFromCommonName(X509Certificate2 certificate)
    {
        var commonName = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
        var separator = commonName.LastIndexOf(':');
        return separator < 0 ? null : commonName[(separator + 1)..];
    }
}

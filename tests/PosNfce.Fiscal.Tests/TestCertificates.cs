using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PosNfce.Fiscal.Tests;

/// <summary>Self-signed certificates shaped like ICP-Brasil e-CNPJ, created on the fly. Same idea as tools/create-dev-certificate.cs.</summary>
internal static class TestCertificates
{
    public const string Password = "test-password";

    public static byte[] Create(string cnpj, DateTimeOffset notAfter, bool cnpjInSubjectAltName = true, string? commonName = null)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={commonName ?? $"EMPRESA TESTE LTDA:{cnpj}"}, O=ICP-Brasil, C=BR", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        if (cnpjInSubjectAltName)
        {
            request.CertificateExtensions.Add(new X509Extension("2.5.29.17", SubjectAltName(cnpj), false));
        }

        using var certificate = request.CreateSelfSigned(notAfter.AddYears(-1), notAfter);
        return certificate.Export(X509ContentType.Pfx, Password);
    }

    private static byte[] SubjectAltName(string cnpj)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);
        using (writer.PushSequence())
        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
        {
            writer.WriteObjectIdentifier("2.16.76.1.3.3");
            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
            {
                writer.WriteOctetString(System.Text.Encoding.ASCII.GetBytes(cnpj));
            }
        }

        return writer.Encode();
    }
}

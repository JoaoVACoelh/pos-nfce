// Creates a self-signed certificate shaped like an ICP-Brasil e-CNPJ A1, for development only.
// SEFAZ rejects it: it only serves the Issuer screen and local XML signing tests.
//
// Usage, from the repository root:  dotnet run tools/create-dev-certificate.cs
// Output: data/dev-certificate.pfx (ignored by Git), password printed on screen.

using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

const string CompanyName = "EMPRESA DE DESENVOLVIMENTO LTDA";
const string Cnpj = "11222333000181"; // fictitious, valid check digits
const string Password = "dev-only";
const string CnpjOid = "2.16.76.1.3.3"; // ICP-Brasil otherName that carries the CNPJ

var output = Path.Combine(Directory.GetCurrentDirectory(), "data", "dev-certificate.pfx");
Directory.CreateDirectory(Path.GetDirectoryName(output)!);

using var key = RSA.Create(2048);
var request = new CertificateRequest(
    $"CN={CompanyName}:{Cnpj}, OU=DEVELOPMENT ONLY, O=ICP-Brasil, C=BR",
    key,
    HashAlgorithmName.SHA256,
    RSASignaturePadding.Pkcs1);

request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
request.CertificateExtensions.Add(new X509KeyUsageExtension(
    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.KeyEncipherment, true));
request.CertificateExtensions.Add(new X509Extension("2.5.29.17", BuildSubjectAltName(Cnpj), false));

using var certificate = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1));
File.WriteAllBytes(output, certificate.Export(X509ContentType.Pfx, Password));

Console.WriteLine($"Certificate: {output}");
Console.WriteLine($"Password:    {Password}");
Console.WriteLine($"CNPJ:        {Cnpj}");
Console.WriteLine($"Valid until: {certificate.NotAfter:yyyy-MM-dd}");

// SubjectAltName with otherName { 2.16.76.1.3.3, [0] OCTET STRING "<CNPJ>" }, as ICP-Brasil certificates do.
static byte[] BuildSubjectAltName(string cnpj)
{
    var writer = new AsnWriter(AsnEncodingRules.DER);
    using (writer.PushSequence())
    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
    {
        writer.WriteObjectIdentifier(CnpjOid);
        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
        {
            writer.WriteOctetString(System.Text.Encoding.ASCII.GetBytes(cnpj));
        }
    }

    return writer.Encode();
}

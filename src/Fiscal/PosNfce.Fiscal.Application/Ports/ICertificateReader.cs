using PosNfce.Fiscal.Domain.Issuers;

namespace PosNfce.Fiscal.Application.Ports;

public interface ICertificateReader
{
    /// <summary>Opens the .pfx with its password. Throws <see cref="SharedKernel.DomainException"/> when the password is wrong or the file isn't an e-CNPJ.</summary>
    CertificateInfo Read(byte[] pfx, string password);
}

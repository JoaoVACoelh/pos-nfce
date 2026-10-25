using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace PosNfce.Fiscal.Adapters.Out.Security;

/// <summary>Encrypts secrets (certificate, its password, CSC) before they reach the database.</summary>
public interface ISecretProtector
{
    byte[] Protect(byte[] plain);

    byte[] Unprotect(byte[] encrypted);
}

/// <summary>
/// Windows DPAPI with machine scope: any Windows user of this computer can open the secrets (cashiers may log in
/// with different accounts), but a copy of the database taken to another computer can't.
/// The entropy ties the data to this app, so another program calling DPAPI on the same blob gets nothing.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PosNfce.Fiscal.Issuer");

    public byte[] Protect(byte[] plain) =>
        ProtectedData.Protect(plain, Entropy, DataProtectionScope.LocalMachine);

    public byte[] Unprotect(byte[] encrypted) =>
        ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.LocalMachine);
}

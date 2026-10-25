namespace PosNfce.Fiscal.Application.Ports;

/// <summary>Keeps the XML files of the NFC-e. Fiscal documents must be kept for at least 5 years.</summary>
public interface IXmlStore
{
    /// <summary>Saves the signed (not yet authorized) XML and returns where it was written.</summary>
    Task<string> SaveSignedAsync(string accessKey, DateTimeOffset issuedAt, string xml, CancellationToken cancellationToken = default);

    /// <summary>Saves the authorized XML (with the SEFAZ protocol) and returns where it was written.</summary>
    Task<string> SaveAuthorizedAsync(string accessKey, DateTimeOffset issuedAt, string xml, CancellationToken cancellationToken = default);
}

using System.Text;
using PosNfce.Fiscal.Application.Ports;

namespace PosNfce.Fiscal.Adapters.Out.Files;

/// <summary>Writes the XML files under &lt;data folder&gt;\xml\YYYY\MM\, the layout the monthly export to the accountant reads.</summary>
public sealed class FileXmlStore(string dataFolder) : IXmlStore
{
    public Task<string> SaveSignedAsync(string accessKey, DateTimeOffset issuedAt, string xml, CancellationToken cancellationToken = default) =>
        SaveAsync($"{accessKey}-assinada.xml", issuedAt, xml, cancellationToken);

    /// <summary>The usual name of the authorized NFC-e file: access key + "-procNFe".</summary>
    public Task<string> SaveAuthorizedAsync(string accessKey, DateTimeOffset issuedAt, string xml, CancellationToken cancellationToken = default) =>
        SaveAsync($"{accessKey}-procNFe.xml", issuedAt, xml, cancellationToken);

    private async Task<string> SaveAsync(string fileName, DateTimeOffset issuedAt, string xml, CancellationToken cancellationToken)
    {
        var folder = Directory.CreateDirectory(Path.Combine(dataFolder, "xml", issuedAt.ToString("yyyy"), issuedAt.ToString("MM"))).FullName;
        var path = Path.Combine(folder, fileName);
        await File.WriteAllTextAsync(path, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
        return path;
    }
}

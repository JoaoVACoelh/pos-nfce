using System.Text;
using PosNfce.Fiscal.Application.Ports;

namespace PosNfce.Fiscal.Adapters.Out.Files;

/// <summary>Writes the XML files under &lt;data folder&gt;\xml\YYYY\MM\, the layout the monthly export to the accountant reads.</summary>
public sealed class FileXmlStore(string dataFolder) : IXmlStore
{
    public async Task<string> SaveSignedAsync(string accessKey, DateTimeOffset issuedAt, string xml, CancellationToken cancellationToken = default)
    {
        var folder = Directory.CreateDirectory(Path.Combine(dataFolder, "xml", issuedAt.ToString("yyyy"), issuedAt.ToString("MM"))).FullName;
        var path = Path.Combine(folder, $"{accessKey}-assinada.xml");
        await File.WriteAllTextAsync(path, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
        return path;
    }
}

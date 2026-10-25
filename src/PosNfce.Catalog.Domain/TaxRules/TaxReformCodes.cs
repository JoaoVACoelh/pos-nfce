using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.TaxRules;

/// <summary>
/// Tax reform codes for the IBS/CBS group: situation code (CST, 3 digits) and tax classification (<c>cClassTrib</c>, 6 digits).
/// Only the format is checked; the official tables are still changing (NT 2025.002-RTC), and the values come from the accountant.
/// </summary>
public sealed record TaxReformCodes
{
    public string IbsCbsCst { get; }
    public string ClassTrib { get; }

    private TaxReformCodes(string ibsCbsCst, string classTrib)
    {
        IbsCbsCst = ibsCbsCst;
        ClassTrib = classTrib;
    }

    public static TaxReformCodes Parse(string? ibsCbsCst, string? classTrib)
    {
        var cst = ibsCbsCst?.Trim() ?? "";
        var classification = classTrib?.Trim() ?? "";

        if (cst.Length != 3 || !cst.All(char.IsAsciiDigit))
        {
            throw new DomainException($"CST de IBS/CBS \"{ibsCbsCst}\" inválido: deve ter 3 dígitos.");
        }

        if (classification.Length != 6 || !classification.All(char.IsAsciiDigit))
        {
            throw new DomainException($"cClassTrib \"{classTrib}\" inválido: deve ter 6 dígitos.");
        }

        return new TaxReformCodes(cst, classification);
    }
}

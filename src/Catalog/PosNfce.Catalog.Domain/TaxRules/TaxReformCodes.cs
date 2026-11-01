using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.TaxRules;

/// <summary>
/// Tax reform data for the IBS/CBS group: situation code (CST, 3 digits), tax classification (<c>cClassTrib</c>, 6 digits)
/// and the rate reduction in percent (<c>pRedAliq</c>; 0 = full rate, e.g. 60 for a 60% reduction).
/// Only the format is checked; the official tables are still changing (NT 2025.002-RTC), and the values come from the accountant.
/// </summary>
public sealed record TaxReformCodes
{
    public string IbsCbsCst { get; }
    public string ClassTrib { get; }
    public Percentage Reduction { get; }

    private TaxReformCodes(string ibsCbsCst, string classTrib, Percentage reduction)
    {
        IbsCbsCst = ibsCbsCst;
        ClassTrib = classTrib;
        Reduction = reduction;
    }

    public static TaxReformCodes Parse(string? ibsCbsCst, string? classTrib, decimal reduction = 0m)
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

        return new TaxReformCodes(cst, classification, Percentage.Of(reduction, "redução do IBS/CBS"));
    }
}

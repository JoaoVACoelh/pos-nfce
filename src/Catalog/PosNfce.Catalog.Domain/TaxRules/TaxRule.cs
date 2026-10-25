using PosNfce.SharedKernel;

namespace PosNfce.Catalog.Domain.TaxRules;

/// <summary>
/// Tax data shared by a group of products (e.g. "Bebidas com ST"), so taxes aren't configured product by product.
/// Catalog only stores it: the calculation lives in the Fiscal module.
/// </summary>
public sealed class TaxRule
{
    public const int NameMaxLength = 60;

    public Guid Id { get; }
    public string Name { get; private set; }
    public Cfop Cfop { get; private set; }
    public Csosn Csosn { get; private set; }
    public PisCofinsCst PisCst { get; private set; }
    public Percentage PisRate { get; private set; }
    public PisCofinsCst CofinsCst { get; private set; }
    public Percentage CofinsRate { get; private set; }
    public TaxReformCodes TaxReform { get; private set; }

    private TaxRule(
        Guid id, string name, Cfop cfop, Csosn csosn,
        PisCofinsCst pisCst, Percentage pisRate, PisCofinsCst cofinsCst, Percentage cofinsRate,
        TaxReformCodes taxReform)
    {
        Id = id;
        Name = ValidName(name);
        Cfop = cfop;
        Csosn = csosn;
        PisCst = pisCst;
        PisRate = pisRate;
        CofinsCst = cofinsCst;
        CofinsRate = cofinsRate;
        TaxReform = taxReform;
    }

    public static TaxRule Register(
        string name, Cfop cfop, Csosn csosn,
        PisCofinsCst pisCst, Percentage pisRate, PisCofinsCst cofinsCst, Percentage cofinsRate,
        TaxReformCodes taxReform) =>
        new(Guid.NewGuid(), name, cfop, csosn, pisCst, pisRate, cofinsCst, cofinsRate, taxReform);

    /// <summary>Rebuilds a rule already stored. Runs the same validations as <see cref="Register"/>.</summary>
    public static TaxRule Restore(
        Guid id, string name, Cfop cfop, Csosn csosn,
        PisCofinsCst pisCst, Percentage pisRate, PisCofinsCst cofinsCst, Percentage cofinsRate,
        TaxReformCodes taxReform) =>
        new(id, name, cfop, csosn, pisCst, pisRate, cofinsCst, cofinsRate, taxReform);

    private static string ValidName(string name)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            throw new DomainException("Nome da regra tributária é obrigatório.");
        }

        if (trimmed.Length > NameMaxLength)
        {
            throw new DomainException($"Nome da regra tributária pode ter no máximo {NameMaxLength} caracteres.");
        }

        return trimmed;
    }
}

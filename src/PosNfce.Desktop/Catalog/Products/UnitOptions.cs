using PosNfce.Desktop.Shared;

namespace PosNfce.Desktop.Catalog.Products;

/// <summary>
/// Common sales units offered in the product form. The NFC-e layout leaves <c>uCom</c> as free text, so these are
/// only suggestions: the operator can still type another code.
/// </summary>
public static class UnitOptions
{
    public static IReadOnlyList<CodeOption> All { get; } =
    [
        new("UN", "Unidade"),
        new("KG", "Quilograma"),
        new("G", "Grama"),
        new("L", "Litro"),
        new("ML", "Mililitro"),
        new("M", "Metro"),
        new("M2", "Metro quadrado"),
        new("CX", "Caixa"),
        new("PCT", "Pacote"),
        new("FD", "Fardo"),
        new("DZ", "Dúzia"),
        new("PC", "Peça"),
        new("PAR", "Par"),
        new("KIT", "Kit"),
    ];
}

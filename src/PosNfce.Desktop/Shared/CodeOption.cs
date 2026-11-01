namespace PosNfce.Desktop.Shared;

/// <summary>
/// A code from an official table with the name the operator reads, for drop-downs (shown by CodeOptionTemplate).
/// The view model keeps only the code; the name is just for the screen.
/// </summary>
public sealed record CodeOption(string Code, string Name)
{
    /// <summary>Also the item's UI Automation name, so tests and screen readers find it by code.</summary>
    public override string ToString() => $"{Code} - {Name}";
}

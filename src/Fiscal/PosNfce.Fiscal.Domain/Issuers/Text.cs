using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>Field checks shared by the issuer types. Messages are shown to the user.</summary>
internal static class Text
{
    public static string Required(string? value, string field, int maxLength) =>
        Optional(value, field, maxLength) ?? throw new DomainException($"{field} é obrigatório.");

    public static string? Optional(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{field} pode ter no máximo {maxLength} caracteres.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string Digits(string? value) => new((value ?? "").Where(char.IsAsciiDigit).ToArray());
}

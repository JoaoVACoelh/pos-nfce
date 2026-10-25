using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>
/// Taxpayer security code (CSC) and its ID, generated on the SEFAZ portal. Used to sign the NFC-e QR Code.
/// It is a secret: stored encrypted, never shown back in full.
/// </summary>
public sealed record Csc
{
    public string TokenId { get; }
    public string Token { get; }

    public Csc(string tokenId, string token)
    {
        TokenId = Text.Digits(tokenId).TrimStart('0');
        if (TokenId.Length is 0 or > 6)
        {
            throw new DomainException("ID do CSC deve ser um número de até 6 dígitos, como aparece no portal da SEFAZ.");
        }

        Token = token?.Trim() ?? "";
        if (Token.Length is 0 or > 36 || !Token.All(char.IsAsciiLetterOrDigit))
        {
            throw new DomainException("CSC deve ter até 36 letras e números, como aparece no portal da SEFAZ.");
        }
    }
}

using PosNfce.SharedKernel;

namespace PosNfce.Fiscal.Domain.Issuers;

/// <summary>
/// Inscrição Estadual check per state. Only MG's check digits are implemented (first client); other states keep the
/// generic 2–14 digit check until a client there needs theirs.
/// </summary>
public static class StateRegistrationRule
{
    /// <returns>The registration as digits only.</returns>
    public static string Validate(string? stateRegistration, BrazilianState state)
    {
        var digits = Text.Digits(stateRegistration);
        if (state.Code == "MG")
        {
            if (digits.Length != 13 || !MinasGeraisCheckDigitsMatch(digits))
            {
                throw new DomainException("Inscrição Estadual de MG inválida: são 13 dígitos e os dois últimos não conferem.");
            }

            return digits;
        }

        if (digits.Length is < 2 or > 14)
        {
            throw new DomainException("Inscrição Estadual deve ter entre 2 e 14 dígitos.");
        }

        return digits;
    }

    /// <summary>
    /// MG layout: 3 digits (municipality) + 6 (registration) + 2 (establishment) + 2 check digits.
    /// First digit: put a "0" after the municipality code, weigh the 12 digits 1, 2, 1, 2…, add the digits of each
    /// product, and take what is missing up to the next multiple of 10.
    /// Second digit: the 11 digits plus the first check digit, weighed 3, 2, 11, 10…2, modulo 11; remainder 0 or 1 gives 0.
    /// </summary>
    private static bool MinasGeraisCheckDigitsMatch(string ie)
    {
        var withZero = ie[..3] + "0" + ie[3..11];
        var sum = 0;
        for (var i = 0; i < withZero.Length; i++)
        {
            var product = (withZero[i] - '0') * (i % 2 == 0 ? 1 : 2);
            sum += product / 10 + product % 10;
        }

        var first = (10 - sum % 10) % 10;

        int[] weights = [3, 2, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2];
        var body = ie[..11] + first;
        var remainder = weights.Select((w, i) => w * (body[i] - '0')).Sum() % 11;
        var second = remainder < 2 ? 0 : 11 - remainder;

        return ie[11] - '0' == first && ie[12] - '0' == second;
    }
}

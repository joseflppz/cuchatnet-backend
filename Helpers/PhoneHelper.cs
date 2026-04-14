using System.Text.RegularExpressions;

namespace CUChatNet.Api.Helpers;

public static class PhoneHelper
{
    public static (string CountryCode, string Number, string FullPhone) Parse(string? rawPhone, string defaultCountryCode = "+506")
    {
        var sanitized = Regex.Replace(rawPhone ?? string.Empty, @"\s+", "");

        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("El teléfono es obligatorio.");

        if (!sanitized.StartsWith("+"))
        {
            sanitized = $"{defaultCountryCode}{sanitized}";
        }

        var digitsOnly = Regex.Replace(sanitized, @"[^\d+]", "");

        if (digitsOnly.Length <= 8)
            throw new ArgumentException("El teléfono debe incluir extensión y número.");

        var localNumber = digitsOnly[^8..];
        var countryCode = digitsOnly[..^8];

        return (countryCode, localNumber, $"{countryCode}{localNumber}");
    }

    public static string GenerateVerificationCode(int length = 6)
    {
        var random = Random.Shared.Next((int)Math.Pow(10, length - 1), (int)Math.Pow(10, length) - 1);
        return random.ToString();
    }
}

namespace dotnet_server._Utils;

public static class PhoneNumberNormalizer
{
    public static bool TryNormalizeUsPhone(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && digits.StartsWith("1"))
        {
            digits = digits[1..];
        }

        if (digits.Length != 10) return false;
        if (!IsPlausible(digits)) return false;

        normalized = $"+1{digits}";
        return true;
    }

    private static bool IsPlausible(string tenDigits)
    {
        if (tenDigits.All(c => c == tenDigits[0])) return false;

        var obviousFakes = new HashSet<string>
        {
            "1234567890",
            "0123456789",
            "9999999999",
            "0000000000",
            "1111111111"
        };

        return !obviousFakes.Contains(tenDigits);
    }
}

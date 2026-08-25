using System.Globalization;

namespace StorePos.Desktop.Common;

public static class DecimalInputParser
{
    private const NumberStyles SupportedStyles =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static bool TryParse(string? input, out decimal value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalizedInput = input.Trim().Replace(',', '.');

        return decimal.TryParse(
            normalizedInput,
            SupportedStyles,
            CultureInfo.InvariantCulture,
            out value);
    }

    public static string Format(decimal value)
        => value.ToString("0.#####", CultureInfo.InvariantCulture);
}

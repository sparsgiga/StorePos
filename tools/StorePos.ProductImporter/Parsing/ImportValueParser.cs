using System.Globalization;
using ClosedXML.Excel;

namespace StorePos.ProductImporter.Parsing;

public static class ImportValueParser
{
    public const decimal MaximumFiveScaleValue = 9999999999999.99999m;

    public static string ReadIdentifier(IXLCell cell)
        => cell.GetFormattedString(CultureInfo.InvariantCulture).Trim();

    public static bool TryReadDecimal(
        IXLCell cell,
        bool allowBlank,
        out decimal? value,
        out string? error)
    {
        value = null;
        error = null;

        if (cell.IsEmpty() || string.IsNullOrWhiteSpace(cell.GetString()))
        {
            if (allowBlank)
            {
                return true;
            }

            error = "Value is required.";
            return false;
        }

        decimal parsed;
        if (cell.DataType == XLDataType.Number)
        {
            if (!cell.TryGetValue<decimal>(out parsed))
            {
                error = "Value is not a valid decimal.";
                return false;
            }
        }
        else
        {
            var text = cell.GetString().Trim();
            if (!TryParseTextDecimal(text, out parsed))
            {
                error = "Value is not a valid decimal.";
                return false;
            }
        }

        parsed = decimal.Round(parsed, 5, MidpointRounding.AwayFromZero);
        if (parsed < 0)
        {
            error = "Value cannot be negative.";
            return false;
        }

        if (parsed > MaximumFiveScaleValue)
        {
            error = "Value exceeds decimal(18,5).";
            return false;
        }

        value = parsed;
        return true;
    }

    private static bool TryParseTextDecimal(string text, out decimal value)
    {
        var normalized = text.Replace(',', '.');
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
    }
}

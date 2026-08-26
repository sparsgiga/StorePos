using System.IO;

namespace StorePos.Desktop.Reporting.Excel;

public static class ReportFileName
{
    private static readonly HashSet<char> InvalidCharacters =
        Path.GetInvalidFileNameChars().ToHashSet();

    public static string ForSale(string saleNumber)
    {
        var sanitized = new string(saleNumber
            .Select(character => InvalidCharacters.Contains(character) ? '_' : character)
            .ToArray())
            .Trim();

        return $"Sale_{(string.IsNullOrWhiteSpace(sanitized) ? "Report" : sanitized)}.xlsx";
    }
}

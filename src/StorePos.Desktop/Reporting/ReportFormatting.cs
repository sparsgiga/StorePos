using System.Globalization;

namespace StorePos.Desktop.Reporting;

public static class ReportFormatting
{
    public static string Quantity(decimal value)
        => value.ToString("0.#####", CultureInfo.InvariantCulture);

    public static string Money(decimal value)
        => value.ToString("N2", CultureInfo.GetCultureInfo("ka-GE"));

    public static string Status(int status)
        => status switch
        {
            1 => "დაუსრულებელი გაყიდვა",
            2 => "დასრულებული",
            3 => "გაუქმებული",
            _ => "უცნობი"
        };
}

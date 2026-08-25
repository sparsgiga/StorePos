using System.Globalization;
using System.Windows.Data;

namespace StorePos.Desktop.Common;

public sealed class DecimalDisplayConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
        => value is decimal decimalValue
            ? DecimalInputParser.Format(decimalValue)
            : string.Empty;

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}

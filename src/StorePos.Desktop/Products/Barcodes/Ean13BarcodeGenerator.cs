namespace StorePos.Desktop.Products.Barcodes;

public sealed class Ean13BarcodeGenerator
{
    public const int BodyLength = 12;
    public const int BarcodeLength = 13;

    public string Generate(string productCode)
    {
        var normalizedCode = productCode?.Trim();
        if (string.IsNullOrEmpty(normalizedCode))
        {
            throw new ArgumentException("Product code is required.", nameof(productCode));
        }

        if (normalizedCode.Any(character => character is < '0' or > '9'))
        {
            throw new ArgumentException(
                "Product code must contain digits only.",
                nameof(productCode));
        }

        if (normalizedCode.Length > BodyLength)
        {
            throw new ArgumentException(
                $"Product code cannot exceed {BodyLength} digits for EAN-13 generation.",
                nameof(productCode));
        }

        var body = normalizedCode.PadLeft(BodyLength, '0');
        var weightedSum = 0;

        for (var index = 0; index < body.Length; index++)
        {
            var digit = body[index] - '0';
            weightedSum += index % 2 == 0 ? digit : digit * 3;
        }

        var checksum = (10 - weightedSum % 10) % 10;
        return $"{body}{checksum}";
    }
}

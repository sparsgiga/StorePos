namespace StorePos.Application.Common.Exceptions;

public sealed class ProductBarcodeConflictException(string barcode)
    : Exception($"A product with barcode '{barcode}' already exists.")
{
    public string Barcode { get; } = barcode;
}

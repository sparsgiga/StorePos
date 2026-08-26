namespace StorePos.Application.Common.Exceptions;

public sealed class ProductBarcodeConflictException(string barcode)
    : Exception($"პროდუქტი შტრიხკოდით „{barcode}“ უკვე არსებობს.")
{
    public string Barcode { get; } = barcode;
}

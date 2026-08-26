namespace StorePos.Desktop.Products;

public enum ProductConflictKind
{
    Code,
    Barcode,
    MeasurementUnit,
    Unknown
}

public sealed class ProductConflictException(
    ProductConflictKind kind,
    string? message = null)
    : Exception(message ?? kind switch
    {
        ProductConflictKind.Code => "ასეთი კოდი უკვე არსებობს.",
        ProductConflictKind.Barcode => "ასეთი შტრიხკოდი უკვე არსებობს.",
        ProductConflictKind.MeasurementUnit => "არჩეული საზომი ერთეული მიუწვდომელია.",
        _ => "პროდუქტის მონაცემები სხვა ჩანაწერთან კონფლიქტშია."
    })
{
    public ProductConflictKind Kind { get; } = kind;
}

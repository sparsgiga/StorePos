namespace StorePos.Desktop.Products;

public enum ProductConflictKind
{
    Code,
    Barcode,
    MeasurementUnit,
    RetailPrice,
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
        ProductConflictKind.RetailPrice =>
            "პროდუქტს საცალო ფასი არ აქვს მითითებული. გაყიდვამდე მიუთითეთ ფასი.",
        _ => "პროდუქტის მონაცემები სხვა ჩანაწერთან კონფლიქტშია."
    })
{
    public ProductConflictKind Kind { get; } = kind;
}

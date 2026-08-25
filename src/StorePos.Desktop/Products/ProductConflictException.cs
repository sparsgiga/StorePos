namespace StorePos.Desktop.Products;

public enum ProductConflictKind
{
    Code,
    Barcode
}

public sealed class ProductConflictException(ProductConflictKind kind)
    : Exception(kind == ProductConflictKind.Code
        ? "A product with this code already exists."
        : "A product with this barcode already exists.")
{
    public ProductConflictKind Kind { get; } = kind;
}

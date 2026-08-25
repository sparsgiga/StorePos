namespace StorePos.Desktop.Products;

public sealed class ProductConflictException()
    : Exception("A product with this barcode already exists.");

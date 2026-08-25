namespace StorePos.Desktop.History.Models;

public sealed record SoldProductsFilter(
    DateTime? DateFrom,
    DateTime? DateTo,
    string? ProductSearch,
    string? SaleNumber,
    string? CustomerName,
    bool? IsManual,
    int PageNumber,
    int PageSize = 50);

public sealed record SoldProductDto(
    long SaleId,
    long SaleItemId,
    string SaleNumber,
    DateTime DateCompleted,
    string? CustomerName,
    long? ProductId,
    string? ProductCode,
    string? Barcode,
    string ProductName,
    string? UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsManual,
    string? Comment)
{
    public string SourceName => IsManual ? "ხელით" : "კატალოგი";
}

public sealed record ManualFilterOption(string Label, bool? Value);

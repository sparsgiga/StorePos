namespace StorePos.Desktop.Sales.Models;

public sealed record DraftSaleItemDto(
    long Id,
    long? ProductId,
    string? ProductCode,
    string? Barcode,
    string ProductName,
    int? UnitId,
    string? UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsManual,
    string? Comment);

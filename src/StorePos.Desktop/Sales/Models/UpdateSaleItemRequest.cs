namespace StorePos.Desktop.Sales.Models;

public sealed record UpdateSaleItemRequest(
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    string? Comment = null);

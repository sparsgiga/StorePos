namespace StorePos.Desktop.Sales.Models;

public sealed record AddManualSaleItemRequest(
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    string? Comment = null);

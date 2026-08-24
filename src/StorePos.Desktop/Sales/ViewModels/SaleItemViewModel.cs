namespace StorePos.Desktop.Sales.ViewModels;

public sealed record SaleItemViewModel(
    long Id,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    bool IsManual,
    string? Comment);

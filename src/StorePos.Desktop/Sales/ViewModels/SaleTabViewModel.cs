namespace StorePos.Desktop.Sales.ViewModels;

public sealed record SaleTabViewModel(
    long Id,
    string SaleNumber,
    decimal TotalAmount,
    DateTime DateCreated,
    string? CustomerName);

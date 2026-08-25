namespace StorePos.Desktop.Sales.Models;

public sealed record DraftSaleDto(
    long Id,
    string SaleNumber,
    decimal TotalAmount,
    DateTime DateCreated,
    long? CustomerId,
    string? CustomerName);

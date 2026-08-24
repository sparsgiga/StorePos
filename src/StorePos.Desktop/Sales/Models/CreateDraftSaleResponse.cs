namespace StorePos.Desktop.Sales.Models;

public sealed record CreateDraftSaleResponse(
    long SaleId,
    string SaleNumber,
    decimal TotalAmount,
    DateTime DateCreated,
    string? CustomerName);

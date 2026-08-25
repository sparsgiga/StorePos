namespace StorePos.Desktop.Sales.Models;

public sealed record RemoveSaleItemResponse(
    long SaleId,
    long SaleItemId,
    decimal SaleTotalAmount);

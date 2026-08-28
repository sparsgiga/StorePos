namespace StorePos.Application.Sales.Queries.GetDrafts;

public sealed record DraftSaleModel(
    long Id,
    string SaleNumber,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    DateTime DateCreated,
    long? CustomerId,
    string? CustomerName);

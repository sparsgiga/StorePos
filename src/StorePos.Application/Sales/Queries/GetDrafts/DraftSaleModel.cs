namespace StorePos.Application.Sales.Queries.GetDrafts;

public sealed record DraftSaleModel(
    long Id,
    string SaleNumber,
    decimal TotalAmount,
    DateTime DateCreated,
    string? CustomerName);

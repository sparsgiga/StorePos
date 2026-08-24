namespace StorePos.Application.Sales.Commands.CreateDraft;

public sealed record CreateDraftSaleResult(
    long SaleId,
    string SaleNumber,
    decimal TotalAmount,
    DateTime DateCreated,
    string? CustomerName);

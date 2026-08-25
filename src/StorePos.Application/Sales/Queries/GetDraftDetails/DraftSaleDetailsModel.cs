namespace StorePos.Application.Sales.Queries.GetDraftDetails;

public sealed record DraftSaleDetailsModel(
    long Id,
    string SaleNumber,
    decimal TotalAmount,
    DateTime DateCreated,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment,
    IReadOnlyList<DraftSaleItemModel> Items);

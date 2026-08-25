namespace StorePos.Desktop.Sales.Models;

public sealed record DraftSaleDetailsDto(
    long Id,
    string SaleNumber,
    decimal TotalAmount,
    DateTime DateCreated,
    long? CustomerId,
    string? CustomerName,
    string? CustomerIdentificationNumber,
    string? Comment,
    IReadOnlyList<DraftSaleItemDto> Items);

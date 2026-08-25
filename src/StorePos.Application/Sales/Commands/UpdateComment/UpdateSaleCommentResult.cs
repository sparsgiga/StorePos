namespace StorePos.Application.Sales.Commands.UpdateComment;

public sealed record UpdateSaleCommentResult(
    long SaleId,
    string? Comment);

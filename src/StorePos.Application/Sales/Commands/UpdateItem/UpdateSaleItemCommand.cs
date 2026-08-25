using MediatR;

namespace StorePos.Application.Sales.Commands.UpdateItem;

public sealed record UpdateSaleItemCommand(
    long SaleId,
    long SaleItemId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    string? Comment = null) : IRequest<UpdateSaleItemResult?>;

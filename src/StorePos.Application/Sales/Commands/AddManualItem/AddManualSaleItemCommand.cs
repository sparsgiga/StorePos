using MediatR;

namespace StorePos.Application.Sales.Commands.AddManualItem;

public sealed record AddManualSaleItemCommand(
    long SaleId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    string? Comment = null) : IRequest<AddManualSaleItemResult?>;

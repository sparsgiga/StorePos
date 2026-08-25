using MediatR;

namespace StorePos.Application.Sales.Commands.RemoveItem;

public sealed record RemoveSaleItemCommand(
    long SaleId,
    long SaleItemId) : IRequest<RemoveSaleItemResult?>;

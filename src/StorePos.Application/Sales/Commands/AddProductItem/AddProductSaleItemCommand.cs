using MediatR;

namespace StorePos.Application.Sales.Commands.AddProductItem;

public sealed record AddProductSaleItemCommand(long SaleId, long ProductId)
    : IRequest<AddProductSaleItemResult?>;

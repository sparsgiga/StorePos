using MediatR;

namespace StorePos.Application.Sales.Commands.UpdateFinancials;

public sealed record UpdateSaleItemFinancialsCommand(
    long SaleId,
    long SaleItemId,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? LineTotal) : IRequest<UpdateSaleItemFinancialsResult?>;

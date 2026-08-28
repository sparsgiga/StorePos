using MediatR;

namespace StorePos.Application.Sales.Commands.UpdateDiscount;

public sealed record UpdateSaleDiscountCommand(
    long SaleId,
    decimal DiscountAmount) : IRequest<UpdateSaleDiscountResult?>;

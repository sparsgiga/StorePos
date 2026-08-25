using MediatR;

namespace StorePos.Application.Sales.Queries.GetDetails;

public sealed record GetSaleDetailsQuery(long SaleId) : IRequest<SaleDetailsModel?>;

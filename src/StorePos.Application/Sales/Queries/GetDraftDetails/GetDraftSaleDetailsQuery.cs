using MediatR;

namespace StorePos.Application.Sales.Queries.GetDraftDetails;

public sealed record GetDraftSaleDetailsQuery(long SaleId)
    : IRequest<DraftSaleDetailsModel?>;

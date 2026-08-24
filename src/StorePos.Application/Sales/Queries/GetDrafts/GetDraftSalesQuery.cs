using MediatR;

namespace StorePos.Application.Sales.Queries.GetDrafts;

public sealed record GetDraftSalesQuery : IRequest<IReadOnlyList<DraftSaleModel>>;

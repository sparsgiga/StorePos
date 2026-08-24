using MediatR;
using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Application.Sales.Queries.GetDrafts;

public sealed class GetDraftSalesQueryHandler(ISaleRepository saleRepository)
    : IRequestHandler<GetDraftSalesQuery, IReadOnlyList<DraftSaleModel>>
{
    public async Task<IReadOnlyList<DraftSaleModel>> Handle(
        GetDraftSalesQuery request,
        CancellationToken cancellationToken)
    {
        var drafts = await saleRepository.GetDraftsAsync(cancellationToken);

        return drafts
            .Select(sale => new DraftSaleModel(
                sale.Id,
                sale.SaleNumber,
                sale.TotalAmount,
                sale.DateCreated,
                sale.CustomerName))
            .ToArray();
    }
}

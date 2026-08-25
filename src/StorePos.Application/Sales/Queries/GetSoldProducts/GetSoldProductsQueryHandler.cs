using MediatR;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Common.Models;

namespace StorePos.Application.Sales.Queries.GetSoldProducts;

public sealed class GetSoldProductsQueryHandler(ISalesReadService salesReadService)
    : IRequestHandler<GetSoldProductsQuery, PagedResult<SoldProductModel>>
{
    public Task<PagedResult<SoldProductModel>> Handle(
        GetSoldProductsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.PageNumber);

        if (request.PageSize is < 1 or > 200)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.PageSize),
                "Page size must be between 1 and 200.");
        }

        if (request.DateFrom > request.DateTo)
        {
            throw new ArgumentException("DateFrom cannot be after DateTo.");
        }

        return salesReadService.GetSoldProductsAsync(request, cancellationToken);
    }
}

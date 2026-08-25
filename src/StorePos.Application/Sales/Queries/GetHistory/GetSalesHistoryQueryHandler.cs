using MediatR;
using StorePos.Application.Common.Interfaces;
using StorePos.Application.Common.Models;

namespace StorePos.Application.Sales.Queries.GetHistory;

public sealed class GetSalesHistoryQueryHandler(ISalesReadService salesReadService)
    : IRequestHandler<GetSalesHistoryQuery, PagedResult<SalesHistoryItemModel>>
{
    public Task<PagedResult<SalesHistoryItemModel>> Handle(
        GetSalesHistoryQuery request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        return salesReadService.GetHistoryAsync(request, cancellationToken);
    }

    private static void Validate(GetSalesHistoryQuery request)
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

        if (request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(request.Status));
        }
    }
}

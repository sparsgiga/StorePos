using MediatR;
using StorePos.Application.Common.Interfaces;

namespace StorePos.Application.Sales.Queries.GetDetails;

public sealed class GetSaleDetailsQueryHandler(ISalesReadService salesReadService)
    : IRequestHandler<GetSaleDetailsQuery, SaleDetailsModel?>
{
    public Task<SaleDetailsModel?> Handle(
        GetSaleDetailsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SaleId);
        return salesReadService.GetDetailsAsync(request.SaleId, cancellationToken);
    }
}

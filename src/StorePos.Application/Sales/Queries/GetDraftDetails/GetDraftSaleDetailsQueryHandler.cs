using MediatR;
using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Application.Sales.Queries.GetDraftDetails;

public sealed class GetDraftSaleDetailsQueryHandler(ISaleRepository saleRepository)
    : IRequestHandler<GetDraftSaleDetailsQuery, DraftSaleDetailsModel?>
{
    public async Task<DraftSaleDetailsModel?> Handle(
        GetDraftSaleDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetDraftDetailsAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        var items = sale.Items
            .OrderBy(item => item.DateCreated)
            .ThenBy(item => item.Id)
            .Select(item => new DraftSaleItemModel(
                item.Id,
                item.ProductId,
                item.ProductCode,
                item.Barcode,
                item.ProductName,
                item.MeasurementUnitId,
                item.MeasurementUnitName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal,
                item.IsManual,
                item.Comment))
            .ToArray();

        return new DraftSaleDetailsModel(
            sale.Id,
            sale.SaleNumber,
            sale.TotalAmount,
            sale.DateCreated,
            sale.CustomerName,
            sale.CustomerIdentificationNumber,
            sale.Comment,
            items);
    }
}

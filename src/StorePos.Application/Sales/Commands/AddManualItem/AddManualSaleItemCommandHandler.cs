using MediatR;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.AddManualItem;

public sealed class AddManualSaleItemCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddManualSaleItemCommand, AddManualSaleItemResult?>
{
    public async Task<AddManualSaleItemResult?> Handle(
        AddManualSaleItemCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetDraftForUpdateAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        var item = sale.AddManualItem(
            request.ProductName,
            request.Quantity,
            request.UnitPrice,
            request.Comment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddManualSaleItemResult(
            sale.Id,
            item.Id,
            item.ProductName,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal,
            sale.TotalAmount,
            item.Comment);
    }
}

using MediatR;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.UpdateItem;

public sealed class UpdateSaleItemCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSaleItemCommand, UpdateSaleItemResult?>
{
    public async Task<UpdateSaleItemResult?> Handle(
        UpdateSaleItemCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SaleId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SaleItemId);

        var sale = await saleRepository.GetDraftForUpdateAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        SaleItem item;

        try
        {
            item = sale.UpdateItem(
                request.SaleItemId,
                request.ProductName,
                request.Quantity,
                request.UnitPrice,
                request.Comment);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateSaleItemResult(
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

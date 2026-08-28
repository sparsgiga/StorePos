using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.RemoveItem;

public sealed class RemoveSaleItemCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveSaleItemCommand, RemoveSaleItemResult?>
{
    public async Task<RemoveSaleItemResult?> Handle(
        RemoveSaleItemCommand request,
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

        try
        {
            sale.RemoveItem(request.SaleItemId);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (InvalidOperationException exception)
        {
            throw new SaleOperationConflictException(
                "ფასდაკლება ვერ იქნება პროდუქტების ჯამზე მეტი.",
                exception);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException exception)
        {
            throw new SaleOperationConflictException(
                "გაყიდვის მდგომარეობა შეიცვალა. განაახლეთ მონაცემები და სცადეთ თავიდან.",
                exception);
        }

        return new RemoveSaleItemResult(
            sale.Id,
            request.SaleItemId,
            sale.TotalAmount);
    }
}

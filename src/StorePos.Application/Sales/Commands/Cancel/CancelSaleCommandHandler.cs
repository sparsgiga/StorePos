using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.Cancel;

public sealed class CancelSaleCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<CancelSaleCommand, CancelSaleResult?>
{
    public async Task<CancelSaleResult?> Handle(
        CancelSaleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SaleId);

        var sale = await saleRepository.GetDraftForUpdateAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        try
        {
            sale.Cancel(timeProvider.GetLocalNow().DateTime);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new SaleOperationConflictException(exception.Message, exception);
        }
        catch (PersistenceConcurrencyException exception)
        {
            throw new SaleOperationConflictException(
                "გაყიდვის ფინანსური მდგომარეობა შეიცვალა. განაახლეთ მონაცემები და სცადეთ თავიდან.",
                exception);
        }

        return new CancelSaleResult(
            sale.Id,
            sale.SaleNumber,
            sale.Status,
            sale.DateCancelled!.Value);
    }
}

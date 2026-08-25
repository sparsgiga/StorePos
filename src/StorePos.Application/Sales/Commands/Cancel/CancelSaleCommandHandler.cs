using MediatR;
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

        sale.Cancel(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CancelSaleResult(
            sale.Id,
            sale.SaleNumber,
            sale.Status,
            sale.DateCancelled!.Value);
    }
}

using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.Reopen;

public sealed class ReopenSaleCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReopenSaleCommand, ReopenSaleResult?>
{
    public async Task<ReopenSaleResult?> Handle(
        ReopenSaleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SaleId);

        var sale = await saleRepository.GetCompletedForUpdateAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        try
        {
            sale.Reopen();
        }
        catch (InvalidOperationException exception)
        {
            throw new SaleOperationConflictException(exception.Message, exception);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReopenSaleResult(
            sale.Id,
            sale.SaleNumber,
            sale.Status,
            sale.TotalAmount);
    }
}

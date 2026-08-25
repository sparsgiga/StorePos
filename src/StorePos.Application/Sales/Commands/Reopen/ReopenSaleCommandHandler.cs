using MediatR;
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

        var sale = await saleRepository.GetCompletedForReopenAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        sale.Reopen();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReopenSaleResult(
            sale.Id,
            sale.SaleNumber,
            sale.Status,
            sale.TotalAmount);
    }
}

using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.Complete;

public sealed class CompleteSaleCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<CompleteSaleCommand, CompleteSaleResult?>
{
    public async Task<CompleteSaleResult?> Handle(
        CompleteSaleCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetDraftForUpdateAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        try
        {
            sale.Complete(
                request.Payments.Select(payment => new SalePaymentAllocation(
                    payment.PaymentType,
                    payment.Amount)),
                timeProvider.GetLocalNow().DateTime,
                request.AllowDebt);
        }
        catch (InvalidOperationException exception)
        {
            throw new SaleOperationConflictException(exception.Message, exception);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException exception)
        {
            throw new SaleOperationConflictException(
                "გაყიდვის ფინანსური მდგომარეობა შეიცვალა. განაახლეთ მონაცემები და სცადეთ თავიდან.",
                exception);
        }

        return new CompleteSaleResult(
            sale.Id,
            sale.SaleNumber,
            sale.Status,
            sale.TotalAmount,
            sale.PaidAmount,
            sale.OutstandingAmount,
            sale.HasDebt,
            sale.DateCompleted!.Value);
    }
}

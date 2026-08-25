using MediatR;
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.SaleId);
        ArgumentNullException.ThrowIfNull(request.Payments);

        if (request.Payments.Count == 0)
        {
            throw new ArgumentException(
                "At least one payment is required.",
                nameof(request.Payments));
        }

        foreach (var payment in request.Payments)
        {
            if (!Enum.IsDefined(payment.PaymentType))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.Payments),
                    "Payment type is not supported.");
            }

            if (payment.Amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.Payments),
                    "Payment amount cannot be negative.");
            }
        }

        var sale = await saleRepository.GetDraftForUpdateAsync(
            request.SaleId,
            cancellationToken);

        if (sale is null)
        {
            return null;
        }

        sale.Complete(
            request.Payments.Select(payment => new SalePaymentAllocation(
                payment.PaymentType,
                payment.Amount)),
            timeProvider.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompleteSaleResult(
            sale.Id,
            sale.SaleNumber,
            sale.Status,
            sale.TotalAmount,
            sale.DateCompleted!.Value);
    }
}

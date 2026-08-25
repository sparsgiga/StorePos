using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Interfaces;

namespace StorePos.Application.Sales.Commands.AddDebtPayment;

public sealed class AddDebtPaymentCommandHandler(
    ISaleRepository saleRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddDebtPaymentCommand, AddDebtPaymentResult?>
{
    public async Task<AddDebtPaymentResult?> Handle(
        AddDebtPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetCompletedForUpdateAsync(
            request.SaleId,
            cancellationToken);
        if (sale is null)
        {
            return null;
        }

        SalePayment payment;
        try
        {
            payment = sale.AddDebtPayment(request.PaymentType, request.Amount);
        }
        catch (InvalidOperationException exception)
        {
            throw new SaleOperationConflictException(exception.Message, exception);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddDebtPaymentResult(
            sale.Id,
            sale.TotalAmount,
            sale.PaidAmount,
            sale.OutstandingAmount,
            sale.HasDebt,
            new SaleDebtPaymentResult(
                payment.PaymentType,
                payment.PaymentKind,
                payment.Amount,
                payment.DateCreated));
    }
}

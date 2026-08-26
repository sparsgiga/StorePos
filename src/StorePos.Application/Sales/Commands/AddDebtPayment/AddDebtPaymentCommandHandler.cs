using MediatR;
using StorePos.Application.Common.Exceptions;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Common;
using StorePos.Domain.Enums;
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
        var normalizedAmount = FinancialPrecision.RoundMoney(request.Amount);
        var operationSale = await saleRepository.GetByDebtPaymentOperationIdAsync(
            request.OperationId,
            cancellationToken);
        if (operationSale is not null)
        {
            var existingPayment = operationSale.Payments.Single(payment =>
                payment.OperationId == request.OperationId);
            if (operationSale.Id != request.SaleId ||
                operationSale.Status != SaleStatus.Completed ||
                existingPayment.CompletionVersion != operationSale.CompletionVersion ||
                existingPayment.PaymentType != request.PaymentType ||
                existingPayment.Amount != normalizedAmount)
            {
                throw new SaleOperationConflictException(
                    "გადახდის ოპერაციის ნომერი უკვე გამოყენებულია განსხვავებული მონაცემებით.");
            }

            return CreateResult(operationSale, existingPayment);
        }

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
            payment = sale.AddDebtPayment(
                request.OperationId,
                request.PaymentType,
                request.Amount);
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

        return CreateResult(sale, payment);
    }

    private static AddDebtPaymentResult CreateResult(Sale sale, SalePayment payment)
        => new(
            sale.Id,
            sale.TotalAmount,
            sale.PaidAmount,
            sale.OutstandingAmount,
            sale.HasDebt,
            new SaleDebtPaymentResult(
                payment.CompletionVersion,
                payment.PaymentType,
                payment.PaymentKind,
                payment.Amount,
                payment.DateCreated));
}

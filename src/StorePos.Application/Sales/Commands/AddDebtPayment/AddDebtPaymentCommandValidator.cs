using FluentValidation;
using StorePos.Domain.Common;

namespace StorePos.Application.Sales.Commands.AddDebtPayment;

public sealed class AddDebtPaymentCommandValidator : AbstractValidator<AddDebtPaymentCommand>
{
    public AddDebtPaymentCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.OperationId).NotEmpty();
        RuleFor(command => command.PaymentType).IsInEnum();
        RuleFor(command => command.Amount)
            .Must(amount =>
            {
                var normalized = FinancialPrecision.RoundMoney(amount);
                return normalized > 0 &&
                       normalized <= FinancialPrecision.MaximumMoneyValue;
            });
    }
}

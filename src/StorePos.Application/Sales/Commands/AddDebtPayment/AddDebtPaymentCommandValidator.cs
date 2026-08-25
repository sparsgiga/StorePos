using FluentValidation;

namespace StorePos.Application.Sales.Commands.AddDebtPayment;

public sealed class AddDebtPaymentCommandValidator : AbstractValidator<AddDebtPaymentCommand>
{
    public AddDebtPaymentCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.PaymentType).IsInEnum();
        RuleFor(command => command.Amount).GreaterThan(0);
    }
}

using FluentValidation;
using StorePos.Domain.Common;

namespace StorePos.Application.Sales.Commands.Complete;

public sealed class CompleteSaleCommandValidator : AbstractValidator<CompleteSaleCommand>
{
    public CompleteSaleCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.Payments).NotNull();
        RuleForEach(command => command.Payments).ChildRules(payment =>
        {
            payment.RuleFor(item => item.PaymentType).IsInEnum();
            payment.RuleFor(item => item.Amount)
                .Must(amount =>
                {
                    var normalized = FinancialPrecision.RoundMoney(amount);
                    return normalized > 0 &&
                           normalized <= FinancialPrecision.MaximumMoneyValue;
                });
        });
    }
}

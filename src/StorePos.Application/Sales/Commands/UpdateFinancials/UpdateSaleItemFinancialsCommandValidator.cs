using FluentValidation;
using StorePos.Domain.Common;

namespace StorePos.Application.Sales.Commands.UpdateFinancials;

public sealed class UpdateSaleItemFinancialsCommandValidator
    : AbstractValidator<UpdateSaleItemFinancialsCommand>
{
    public UpdateSaleItemFinancialsCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.SaleItemId).GreaterThan(0);
        RuleFor(command => command)
            .Must(command => new[]
            {
                command.Quantity.HasValue,
                command.UnitPrice.HasValue,
                command.LineTotal.HasValue
            }.Count(hasValue => hasValue) == 1)
            .WithMessage("ზუსტად ერთი ფინანსური ველი უნდა შეიცვალოს.");
        RuleFor(command => command.Quantity!.Value)
            .GreaterThan(0)
            .LessThanOrEqualTo(FinancialPrecision.MaximumFiveScaleValue)
            .When(command => command.Quantity.HasValue);
        RuleFor(command => command.UnitPrice!.Value)
            .GreaterThanOrEqualTo(0.00001m)
            .LessThanOrEqualTo(FinancialPrecision.MaximumFiveScaleValue)
            .When(command => command.UnitPrice.HasValue);
        RuleFor(command => command.LineTotal!.Value)
            .GreaterThanOrEqualTo(0.01m)
            .LessThanOrEqualTo(FinancialPrecision.MaximumMoneyValue)
            .When(command => command.LineTotal.HasValue);
    }
}

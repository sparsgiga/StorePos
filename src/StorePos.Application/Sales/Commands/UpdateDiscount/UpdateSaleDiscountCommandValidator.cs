using FluentValidation;
using StorePos.Domain.Common;

namespace StorePos.Application.Sales.Commands.UpdateDiscount;

public sealed class UpdateSaleDiscountCommandValidator
    : AbstractValidator<UpdateSaleDiscountCommand>
{
    public UpdateSaleDiscountCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.DiscountAmount)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(FinancialPrecision.MaximumMoneyValue);
    }
}

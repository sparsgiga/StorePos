using FluentValidation;
using StorePos.Domain.Common;

namespace StorePos.Application.Products.Commands.UpdateRetailPrice;

public sealed class UpdateProductRetailPriceCommandValidator
    : AbstractValidator<UpdateProductRetailPriceCommand>
{
    public UpdateProductRetailPriceCommandValidator()
    {
        RuleFor(command => command.ProductId).GreaterThan(0);
        RuleFor(command => command.Price)
            .Must(price =>
            {
                var normalized = FinancialPrecision.RoundUnitPrice(price);
                return normalized > 0 &&
                       normalized <= FinancialPrecision.MaximumFiveScaleValue;
            })
            .WithMessage("მიუთითეთ 0-ზე მეტი საცალო ფასი.");
    }
}

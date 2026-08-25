using FluentValidation;

namespace StorePos.Application.Sales.Commands.AddProductItem;

public sealed class AddProductSaleItemCommandValidator
    : AbstractValidator<AddProductSaleItemCommand>
{
    public AddProductSaleItemCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.ProductId).GreaterThan(0);
    }
}

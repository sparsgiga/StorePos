using FluentValidation;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Application.Products.Commands.CreateAndAddToSale;

public sealed class CreateProductAndAddToSaleCommandValidator
    : AbstractValidator<CreateProductAndAddToSaleCommand>
{
    public CreateProductAndAddToSaleCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Product.NameMaxLength);
        RuleFor(command => command.Barcode)
            .MaximumLength(Product.BarcodeMaxLength);
        RuleFor(command => command.MeasurementUnitId).GreaterThan(0);
        RuleFor(command => command.Quantity).GreaterThan(0);
        RuleFor(command => command.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Comment)
            .MaximumLength(SaleItem.CommentMaxLength);
    }
}

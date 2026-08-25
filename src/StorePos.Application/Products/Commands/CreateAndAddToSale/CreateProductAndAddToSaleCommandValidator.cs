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
        RuleFor(command => command.ProductCode)
            .NotEmpty()
            .MaximumLength(Product.CodeMaxLength)
            .Matches("^[0-9]+$")
            .WithMessage("Product code must contain digits only.");
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Product.NameMaxLength);
        RuleFor(command => command.Barcode)
            .NotEmpty()
            .MaximumLength(Product.BarcodeMaxLength);
        RuleFor(command => command.MeasurementUnitId).GreaterThan(0);
        RuleFor(command => command.Quantity).GreaterThan(0);
        RuleFor(command => command.UnitPrice)
            .GreaterThanOrEqualTo(Product.MinimumPrice);
        RuleFor(command => command.Comment)
            .MaximumLength(SaleItem.CommentMaxLength);
    }
}

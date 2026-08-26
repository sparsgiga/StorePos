using FluentValidation;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Application.Sales.Commands.AddManualItem;

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
        RuleFor(command => command.Quantity)
            .Must(AddManualSaleItemCommandValidator.IsValidQuantity);
        RuleFor(command => command.UnitPrice)
            .Must(AddManualSaleItemCommandValidator.IsValidUnitPrice);
        RuleFor(command => command)
            .Must(command => AddManualSaleItemCommandValidator.CanCalculateLineTotal(
                command.Quantity,
                command.UnitPrice))
            .WithName("LineTotal");
        RuleFor(command => command.Comment)
            .MaximumLength(SaleItem.CommentMaxLength);
    }
}

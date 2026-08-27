using FluentValidation;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Common;

namespace StorePos.Application.Products.Commands.Create;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("პროდუქტის დასახელება სავალდებულოა.")
            .MaximumLength(Product.NameMaxLength);
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("პროდუქტის კოდი სავალდებულოა.")
            .MaximumLength(Product.CodeMaxLength)
            .Matches("^[0-9]+$")
            .WithMessage("ახალი პროდუქტის კოდი უნდა შეიცავდეს მხოლოდ ციფრებს (0-9).");
        RuleFor(command => command.Barcode)
            .MaximumLength(Product.BarcodeMaxLength);
        RuleFor(command => command.SupplierName)
            .MaximumLength(Product.SupplierNameMaxLength);
        RuleFor(command => command.SupplierCode)
            .MaximumLength(Product.SupplierCodeMaxLength);
        RuleFor(command => command.MeasurementUnitId)
            .GreaterThan(0).WithMessage("მიუთითეთ საზომი ერთეული.");
        RuleFor(command => command.Price)
            .Must(price =>
            {
                var normalized = FinancialPrecision.RoundUnitPrice(price);
                return normalized >= Product.MinimumPrice &&
                       normalized <= FinancialPrecision.MaximumFiveScaleValue;
            })
            .WithMessage("საცალო ფასი არ შეიძლება იყოს უარყოფითი ან დასაშვებ დიაპაზონს აღემატებოდეს.");
        RuleFor(command => command.CostPrice)
            .Must(costPrice =>
            {
                if (!costPrice.HasValue)
                {
                    return true;
                }

                var normalized = FinancialPrecision.RoundUnitPrice(costPrice.Value);
                return normalized >= 0 &&
                       normalized <= FinancialPrecision.MaximumFiveScaleValue;
            })
            .WithMessage("პირველადი ფასი არ შეიძლება იყოს უარყოფითი ან დასაშვებ დიაპაზონს აღემატებოდეს.");
    }
}

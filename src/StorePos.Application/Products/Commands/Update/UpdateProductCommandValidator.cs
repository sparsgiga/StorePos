using FluentValidation;
using StorePos.Domain.Aggregates.Product;
using StorePos.Domain.Common;

namespace StorePos.Application.Products.Commands.Update;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId).GreaterThan(0);
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("პროდუქტის დასახელება სავალდებულოა.")
            .MaximumLength(Product.NameMaxLength);
        RuleFor(command => command.Code)
            .NotEmpty().WithMessage("პროდუქტის კოდი სავალდებულოა.")
            .MaximumLength(Product.CodeMaxLength);
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

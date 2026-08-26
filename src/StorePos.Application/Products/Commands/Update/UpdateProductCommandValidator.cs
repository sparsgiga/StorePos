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
            .MaximumLength(Product.CodeMaxLength)
            .Matches("^[0-9]+$").WithMessage("კოდი უნდა შეიცავდეს მხოლოდ ციფრებს.");
        RuleFor(command => command.Barcode)
            .NotEmpty().WithMessage("შტრიხკოდი სავალდებულოა.")
            .MaximumLength(Product.BarcodeMaxLength);
        RuleFor(command => command.MeasurementUnitId)
            .GreaterThan(0).WithMessage("მიუთითეთ საზომი ერთეული.");
        RuleFor(command => command.Price)
            .Must(price =>
            {
                var normalized = FinancialPrecision.RoundUnitPrice(price);
                return normalized >= Product.MinimumPrice &&
                       normalized <= FinancialPrecision.MaximumFiveScaleValue;
            })
            .WithMessage("ფასი უნდა იყოს ნულზე მეტი და დასაშვებ დიაპაზონში.");
    }
}

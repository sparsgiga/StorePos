using FluentValidation;
using StorePos.Application.Sales.Commands.AddManualItem;
using StorePos.Domain.Aggregates.Sale;

namespace StorePos.Application.Sales.Commands.UpdateItem;

public sealed class UpdateSaleItemCommandValidator
    : AbstractValidator<UpdateSaleItemCommand>
{
    public UpdateSaleItemCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.SaleItemId).GreaterThan(0);
        RuleFor(command => command.ProductName)
            .NotEmpty()
            .MaximumLength(SaleItem.ProductNameMaxLength);
        RuleFor(command => command.Quantity)
            .Must(AddManualSaleItemCommandValidator.IsValidQuantity)
            .WithMessage("რაოდენობა უნდა იყოს ნულზე მეტი და დასაშვებ დიაპაზონში.");
        RuleFor(command => command.UnitPrice)
            .Must(AddManualSaleItemCommandValidator.IsValidUnitPrice)
            .WithMessage("ფასი უნდა იყოს ნულზე მეტი და დასაშვებ დიაპაზონში.");
        RuleFor(command => command)
            .Must(command => AddManualSaleItemCommandValidator.CanCalculateLineTotal(
                command.Quantity,
                command.UnitPrice))
            .WithName("LineTotal")
            .WithMessage("რაოდენობისა და ფასის ნამრავლი უნდა იყოს მინიმუმ 0.01 ₾ და დასაშვებ დიაპაზონში.");
        RuleFor(command => command.Comment).MaximumLength(SaleItem.CommentMaxLength);
    }
}

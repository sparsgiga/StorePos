using FluentValidation;
using StorePos.Domain.Aggregates.Sale;
using StorePos.Domain.Common;

namespace StorePos.Application.Sales.Commands.AddManualItem;

public sealed class AddManualSaleItemCommandValidator
    : AbstractValidator<AddManualSaleItemCommand>
{
    public AddManualSaleItemCommandValidator()
    {
        RuleFor(command => command.SaleId).GreaterThan(0);
        RuleFor(command => command.ProductName)
            .NotEmpty()
            .MaximumLength(SaleItem.ProductNameMaxLength);
        RuleFor(command => command.Quantity)
            .Must(IsValidQuantity)
            .WithMessage("რაოდენობა უნდა იყოს ნულზე მეტი და დასაშვებ დიაპაზონში.");
        RuleFor(command => command.UnitPrice)
            .Must(IsValidUnitPrice)
            .WithMessage("ფასი უნდა იყოს ნულზე მეტი და დასაშვებ დიაპაზონში.");
        RuleFor(command => command)
            .Must(command => CanCalculateLineTotal(command.Quantity, command.UnitPrice))
            .WithName("LineTotal")
            .WithMessage("რაოდენობისა და ფასის ნამრავლი უნდა იყოს მინიმუმ 0.01 ₾ და დასაშვებ დიაპაზონში.");
        RuleFor(command => command.Comment).MaximumLength(SaleItem.CommentMaxLength);
    }

    internal static bool IsValidQuantity(decimal value)
    {
        var normalized = FinancialPrecision.RoundQuantity(value);
        return normalized > 0 && normalized <= FinancialPrecision.MaximumFiveScaleValue;
    }

    internal static bool IsValidUnitPrice(decimal value)
    {
        var normalized = FinancialPrecision.RoundUnitPrice(value);
        return normalized >= SaleItem.MinimumUnitPrice &&
               normalized <= FinancialPrecision.MaximumFiveScaleValue;
    }

    internal static bool CanCalculateLineTotal(decimal quantity, decimal unitPrice)
    {
        try
        {
            FinancialPrecision.CalculateLineTotal(quantity, unitPrice);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentOutOfRangeException or OverflowException)
        {
            return false;
        }
    }
}

using FluentValidation;

namespace StorePos.Application.Products.Commands.Deactivate;

public sealed class DeactivateProductCommandValidator
    : AbstractValidator<DeactivateProductCommand>
{
    public DeactivateProductCommandValidator()
        => RuleFor(command => command.ProductId).GreaterThan(0);
}

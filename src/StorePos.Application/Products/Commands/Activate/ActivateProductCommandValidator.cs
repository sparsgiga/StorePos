using FluentValidation;

namespace StorePos.Application.Products.Commands.Activate;

public sealed class ActivateProductCommandValidator : AbstractValidator<ActivateProductCommand>
{
    public ActivateProductCommandValidator()
        => RuleFor(command => command.ProductId).GreaterThan(0);
}

using FluentValidation;
using StorePos.Domain.Aggregates.Customer;

namespace StorePos.Application.Customers.Commands.Create;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Customer.NameMaxLength);
        RuleFor(command => command.IdentificationNumber)
            .MaximumLength(Customer.IdentificationNumberMaxLength);
        RuleFor(command => command.Information)
            .MaximumLength(Customer.InformationMaxLength);
    }
}

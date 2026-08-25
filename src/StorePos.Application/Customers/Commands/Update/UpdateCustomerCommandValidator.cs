using FluentValidation;
using StorePos.Domain.Aggregates.Customer;

namespace StorePos.Application.Customers.Commands.Update;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(command => command.CustomerId).GreaterThan(0);
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Customer.NameMaxLength);
        RuleFor(command => command.IdentificationNumber)
            .MaximumLength(Customer.IdentificationNumberMaxLength);
        RuleFor(command => command.Information)
            .MaximumLength(Customer.InformationMaxLength);
    }
}
